# Request Tracing

## Overview

The application assigns a unique `X-Request-Id` to every HTTP request.

If the client already provides an `X-Request-Id` header, the same identifier is propagated throughout the request pipeline. Otherwise, a new GUID is generated.

The request identifier is:

- Added to the response as the `X-Request-Id` header.
- Included in the Serilog logging context.
- Shared across all log entries generated during the request.

## Usage

Example:

Request

GET /github

Response

HTTP/1.1 302 Found
X-Request-Id: 687b4673-dab4-489a-8ee2-e96841723fdf

The same identifier can then be used to locate all log entries related to that request.