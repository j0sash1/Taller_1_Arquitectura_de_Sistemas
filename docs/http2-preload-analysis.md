# HTTP/2 Server Push Analysis and Preload

## Overview

HTTP/2 introduced **Server Push** as a mechanism that allows the server to proactively send resources (such as CSS, JavaScript, or images) before the browser explicitly requests them. The goal was to reduce page load time by avoiding additional round trips.

However, modern browsers have largely deprecated or removed support for HTTP/2 Server Push because it often resulted in unnecessary bandwidth usage. Servers could push resources that were already cached by the client, leading to reduced efficiency instead of performance improvements.

## Why Preload?

The recommended alternative is the **Preload** resource hint.

Using the HTML tag:

```html
<link rel="preload" href="..." as="style">
```

allows the browser to prioritize downloading critical resources while still making its own caching decisions. Unlike Server Push, preload does not force resources to be sent by the server, making it more efficient and widely supported.

## Project Implementation

For this project, preload hints were added for the application's critical CSS resources:

- Bulma CSS framework
- Custom `site.css` stylesheet

Example:

```html
<link rel="preload"
      href="https://cdn.jsdelivr.net/npm/bulma@1.0.4/css/bulma.min.css"
      as="style">

<link rel="preload"
      href="~/css/site.css"
      as="style"
      asp-append-version="true">
```

The corresponding stylesheet links remain unchanged so the browser applies the styles normally after downloading them.

## Validation

The application was executed locally and the generated HTML source was inspected. The `<head>` section now contains the preload hints before the stylesheet declarations, confirming that critical CSS resources are prioritized during page loading.

## Conclusion

Although HTTP/2 Server Push was designed to improve web performance, it is no longer recommended due to limited browser support and inefficient resource delivery. HTML preload provides a simpler, standards-compliant, and browser-supported mechanism for prioritizing critical assets. Therefore, preload was selected as the implementation strategy for this project.