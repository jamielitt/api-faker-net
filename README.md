# api-faker-net

The API Faker, but in .NET Core.

api-faker-net is a lightweight fake HTTP API for local development and testing. Point your application at it instead of a real API, then tell it, using a few special `x-` headers, what each endpoint should return: the body, the status code, and how long to take. Anything you haven't faked is passed straight through to a real API of your choice.

Use it to:

- **Build against an API that doesn't exist yet**: fake the endpoints you need and carry on.
- **Test the awkward cases**: return 500s, 429s, slow responses or unusual payloads on demand.
- **Script multi-step flows**: queue up several responses for one endpoint and have them returned in order.
- **Fake only part of an API**: override the endpoints you care about and let everything else go to the real thing.

## Contents

- [How it works](#how-it-works)
- [Getting started](#getting-started)
- [Control headers](#control-headers)
- [Examples](#examples)
- [Proxying to a real API](#proxying-to-a-real-api)
- [Configuration](#configuration)
- [Good to know](#good-to-know)
- [Project structure](#project-structure)

## How it works

Every request, to any path and with any HTTP verb, is handled in this order:

1. **Store.** If the request has an `x-set-response` header, its body is saved as the response for that verb and path. Nothing else happens.
2. **Diagnostic.** If the request has an `x-diagnostic` header, the faker describes how it read the request instead of answering it.
3. **Replay.** If a response has been stored for the verb and path, it's returned.
4. **Proxy.** Otherwise, the request is forwarded to the configured `ProxyTarget` and the real API's response is returned.

Stored responses are matched on **verb + path**. `GET /users/42` and `DELETE /users/42` are separate endpoints.

## Getting started

You'll need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/jamielitt/api-faker-net.git
cd api-faker-net
dotnet run --project api-faker-net
```

The faker listens on **http://localhost:5254**. In Development, unfaked requests are proxied to `https://api.restful-api.dev` (see [Configuration](#configuration) to change that).

Try it:

```bash
# Fake GET /hello
curl -X GET http://localhost:5254/hello \
  -H 'x-set-response;' \
  -H 'Content-Type: text/plain' \
  -d 'Hello from the faker!'

# Call it
curl http://localhost:5254/hello
# Hello from the faker!
```

## Control headers

| Header | Value | Used when | What it does |
|---|---|---|---|
| `x-set-response` | Anything, including empty | Storing | Store this request's body as the response for this verb and path. Only the header's presence matters. |
| `x-set-httpresponsecode` | Status code, `200`–`599` | Storing | The status code to return. Defaults to `200`. |
| `x-delay` | Milliseconds, `0` or more | Storing | How long to wait before returning the response. Defaults to `0`. |
| `x-sequence` | Integer | Storing and reading | When storing, the response's position in the queue (defaults to `0`). When reading, return that specific response. |
| `x-diagnostic` | Anything, including empty | Reading | Describe how the request's control headers were read, instead of returning a response. |

The response's `Content-Type` is taken from the storing request's `Content-Type` header, and defaults to `application/json`.

Control headers are never forwarded to the real API when a request is proxied.

> **curl tip:** `-H 'x-set-response:'` (with a colon) tells curl to *remove* the header. To send it with an empty value, use a semicolon: `-H 'x-set-response;'`. Or give it any value, e.g. `-H 'x-set-response: true'`.

## Examples

The examples use curl. The same requests are in [`api-faker-net/http/`](api-faker-net/http/) as `.http` files you can run from JetBrains Rider or the VS Code REST Client.

### Fake a GET endpoint

To fake a verb, store the response *with that verb*. To fake a `GET`, send a `GET` that has a body:

```bash
curl -X GET http://localhost:5254/users/42 \
  -H 'x-set-response;' \
  -H 'Content-Type: application/json' \
  -d '{"id": 42, "name": "Ada Lovelace"}'
```

The faker confirms what it stored:

```json
{"stored":true,"httpVerb":"GET","path":"/users/42","sequence":0,"statusCode":200,"delayMs":0,"bodyLength":34}
```

Now any `GET /users/42` returns it:

```bash
curl -i http://localhost:5254/users/42
# HTTP/1.1 200 OK
# Content-Type: application/json
#
# {"id": 42, "name": "Ada Lovelace"}
```

### Fake a POST with a specific status code

```bash
curl -X POST http://localhost:5254/signup/start \
  -H 'x-set-response;' \
  -H 'x-set-httpresponsecode: 201' \
  -H 'Content-Type: application/json' \
  -d '{"userId": "abc-123", "status": "pending"}'
```

Every `POST /signup/start` now returns `201 Created` with that body, whatever body the caller sends.

### Simulate an error

```bash
curl -X GET http://localhost:5254/orders \
  -H 'x-set-response;' \
  -H 'x-set-httpresponsecode: 503' \
  -H 'Content-Type: application/json' \
  -d '{"error": "Service temporarily unavailable"}'
```

For a response with no body, such as `204 No Content`, leave out `-d`:

```bash
curl -X DELETE http://localhost:5254/users/42 \
  -H 'x-set-response;' \
  -H 'x-set-httpresponsecode: 204'
```

### Simulate a slow response

```bash
curl -X GET http://localhost:5254/reports/annual \
  -H 'x-set-response;' \
  -H 'x-delay: 3000' \
  -d '{"status": "ready"}'

time curl http://localhost:5254/reports/annual
# {"status": "ready"}
# real    0m3.0s
```

Delays don't tie up the server, so many slow requests can run at once. Useful for testing timeouts and loading states.

### Return a sequence of responses

Store several responses for one endpoint using `x-sequence`. Without an `x-sequence` header, reads cycle through them in sequence order and then start again:

```bash
curl -X GET http://localhost:5254/jobs/7 -H 'x-set-response;' -H 'x-sequence: 1' -d '{"status": "queued"}'
curl -X GET http://localhost:5254/jobs/7 -H 'x-set-response;' -H 'x-sequence: 2' -d '{"status": "running"}'
curl -X GET http://localhost:5254/jobs/7 -H 'x-set-response;' -H 'x-sequence: 3' -d '{"status": "complete"}'

curl http://localhost:5254/jobs/7   # {"status": "queued"}
curl http://localhost:5254/jobs/7   # {"status": "running"}
curl http://localhost:5254/jobs/7   # {"status": "complete"}
curl http://localhost:5254/jobs/7   # {"status": "queued"}  (back to the start)
```

This is handy for polling flows, retries (fail, fail, succeed) and pagination. Each response in a sequence can have its own status code and delay.

To get one specific response, ask for it by number:

```bash
curl http://localhost:5254/jobs/7 -H 'x-sequence: 3'
# {"status": "complete"}
```

Storing a response with a sequence number that already exists replaces it.

### See how a request is interpreted

`x-diagnostic` shows how the faker reads your control headers, which is useful when a request isn't behaving as expected:

```bash
curl http://localhost:5254/users/42 \
  -H 'x-diagnostic;' \
  -H 'x-sequence: 2' \
  -H 'x-delay: 250'
```

```json
{
  "httpVerb": "GET",
  "path": "/users/42",
  "response": "x-set-response header not set. This will be used to load the cache for this endpoint with data",
  "delay": "250",
  "sequence": "2",
  "httpResponseCode": "x-set-httpresponsecode header not set"
}
```

The diagnostic response uses the `x-set-httpresponsecode` status if one is given, otherwise `404`.

### Invalid header values

A control header with a bad value is rejected with `400 Bad Request` and an explanation:

```bash
curl http://localhost:5254/users/42 -H 'x-sequence: first'
```

```json
{"error":"Invalid value for header 'x-sequence'. Expected an integer.","header":"x-sequence","value":"first"}
```

## Proxying to a real API

When nothing is stored for a request's verb and path, the faker forwards it to the `ProxyTarget` and returns the real response: status, headers and body.

```bash
# Nothing stored for GET /objects/7, so this goes to https://api.restful-api.dev/objects/7
curl http://localhost:5254/objects/7
```

When proxying:

- The method, path, query string, body and headers are forwarded. The `x-` control headers are removed first.
- Redirects are passed back to the caller rather than followed.
- Cookies are never shared between callers. Each caller's own `Cookie` header is forwarded as-is.

If no `ProxyTarget` is configured, unfaked requests get `502 Bad Gateway`:

```json
{"error":"No stored response found and no ProxyTarget is configured.","httpVerb":"GET","path":"/objects/7"}
```

## Configuration

Settings live in the `AppConfiguration` section of `appsettings.json` (or `appsettings.Development.json` for Development):

```json
{
  "AppConfiguration": {
    "ProxyTarget": "https://api.restful-api.dev"
  }
}
```

| Setting | Description |
|---|---|
| `ProxyTarget` | Base URL of the real API that unfaked requests are forwarded to. Leave it unset to disable proxying. |

Settings can also be given as environment variables, using a double underscore between the section and the setting name:

```bash
AppConfiguration__ProxyTarget=https://staging.example.com dotnet run --project api-faker-net
```

`ProxyTarget` is only set in `appsettings.Development.json`. Outside Development, set it yourself or proxying is disabled.

## Good to know

- **Stored responses are in memory.** They're lost when the faker restarts, and are shared by everyone calling the same instance.
- **Paths must match exactly.** Matching is case-sensitive and a trailing slash matters, so `/users/42`, `/Users/42` and `/users/42/` are three different endpoints.
- **Query strings are ignored when matching.** A response stored for `GET /users` is also returned for `GET /users?page=2`. Use `x-sequence` if you need different responses.
- **There's no way to delete a stored response** other than restarting the faker. You can replace one by storing again with the same `x-sequence`.
- **`204` and `304` responses never have a body**, even if one was stored, because HTTP doesn't allow it.

## Project structure

```
api-faker-net/
├── Program.cs                  App startup: AddApiFaker() and MapApiFaker()
├── Configuration/              AppConfiguration options, service registration and endpoint mapping
├── Handlers/                   RequestHandler: store → diagnostic → replay → proxy
├── Requests/                   Control header names and the parser that validates them
├── Storage/                    In-memory store of responses, with sequencing and round-robin
├── Proxy/                      Forwards unfaked requests to the ProxyTarget
├── Models/                     StoredResponse
└── http/                       Example requests (.http files)
```
