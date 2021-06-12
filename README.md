# Reverse Proxy

This is a toy reverse proxy project. Not intended for production use.

## How does it work?

A reverse proxy is basically two things, an HTTP server and an HTTP client. Which act as a middleman for another Client and Server

```
|--------|         |--------------|         |--------|
|        |  ---->  |              |  ---->  |        |
| Client |         | ReverseProxy |         | Server |
|        |  <----  |              |  <----  |        |
|--------|         |--------------|         |--------|
```


1. Listen on a Socket
2. Parse incoming HTTP requests
3. Based on some configuration, work out what to do with the request.
4. If configured, send request on to the upstream server
5. Send the response to the Client
