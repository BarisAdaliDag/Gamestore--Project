const http = require("node:http");
const fs = require("node:fs/promises");
const path = require("node:path");

const rootDirectory = __dirname;
const host = process.env.HOST || "127.0.0.1";
const port = Number(process.env.PORT || 4200);

const contentTypes = {
  ".css": "text/css; charset=utf-8",
  ".html": "text/html; charset=utf-8",
  ".ico": "image/x-icon",
  ".js": "text/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".map": "application/json; charset=utf-8",
  ".png": "image/png",
  ".svg": "image/svg+xml",
  ".woff": "font/woff",
  ".woff2": "font/woff2",
};

function isInsideRoot(filePath) {
  return filePath === rootDirectory || filePath.startsWith(`${rootDirectory}${path.sep}`);
}

async function resolveFilePath(requestPath) {
  const decodedPath = decodeURIComponent(requestPath);
  const requestedFile = path.resolve(rootDirectory, `.${decodedPath}`);

  if (!isInsideRoot(requestedFile)) {
    return null;
  }

  try {
    const stats = await fs.stat(requestedFile);
    return stats.isDirectory() ? path.join(requestedFile, "index.html") : requestedFile;
  } catch {
    return path.extname(decodedPath) ? null : path.join(rootDirectory, "index.html");
  }
}

const server = http.createServer(async (request, response) => {
  if (request.method !== "GET" && request.method !== "HEAD") {
    response.writeHead(405, { Allow: "GET, HEAD" });
    response.end();
    return;
  }

  try {
    const requestUrl = new URL(request.url, `http://${request.headers.host || host}`);
    const filePath = await resolveFilePath(requestUrl.pathname);

    if (!filePath) {
      response.writeHead(404);
      response.end("Not found");
      return;
    }

    const content = await fs.readFile(filePath);
    const contentType = contentTypes[path.extname(filePath).toLowerCase()] || "application/octet-stream";

    response.writeHead(200, {
      "Cache-Control": "no-store",
      "Content-Type": contentType,
    });

    response.end(request.method === "HEAD" ? undefined : content);
  } catch (error) {
    response.writeHead(500);
    response.end("Server error");
    console.error(error);
  }
});

server.on("error", (error) => {
  if (error.code === "EADDRINUSE") {
    console.error(`Port ${port} is already in use. Set a different port with the PORT environment variable.`);
  } else {
    console.error(error);
  }

  process.exitCode = 1;
});

server.listen(port, host, () => {
  console.log(`Game Store UI: http://${host}:${port}`);
  console.log("Stop the server with Ctrl+C.");
});
