import { defineConfig, ProxyOptions } from "vite";
import react from "@vitejs/plugin-react-swc";
import plugin from "@vitejs/plugin-react";
import fs from "fs";
import path from "path";
import child_process from "child_process";
import { env } from "process";

const baseFolder =
  env.APPDATA !== undefined && env.APPDATA !== ""
    ? `${env.APPDATA}/ASP.NET/https`
    : `${env.HOME}/.aspnet/https`;

const certificateName = "asd.client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(baseFolder)) {
  fs.mkdirSync(baseFolder, { recursive: true });
}

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
  if (
    0 !==
    child_process.spawnSync(
      "dotnet",
      [
        "dev-certs",
        "https",
        "--export-path",
        certFilePath,
        "--format",
        "Pem",
        "--no-password",
      ],
      { stdio: "inherit" }
    ).status
  ) {
    throw new Error("Could not create certificate.");
  }
}

const port = env.ASPNETCORE_LOCAL_LAUNCH ? 42522 : 3000;

const target = env.ASPNETCORE_HTTPS_PORT
  ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
  : env.ASPNETCORE_URLS
  ? env.ASPNETCORE_URLS.split(";")[0]
  : "https://localhost:7261";

const proxyConfig: ProxyOptions = {
  target: target, // <-- your .NET process
  secure: false,
  configure: (proxy) => {
    proxy.on("error", (err) => {
      console.log("proxy error", err);
    });
    proxy.on("proxyReq", (_, req) => {
      console.log("Sending Request to .NET:", req.method, req.url);
    });
    proxy.on("proxyRes", (proxyRes, req) => {
      console.log("Received Response from .NET:", proxyRes.statusCode, req.url);
    });
  },
};

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), plugin()],
  server: {
    host: true,
    port: port,
    proxy: {
      "*": proxyConfig,
    },
  },
  build: {
    outDir: "dist",
  },
});
