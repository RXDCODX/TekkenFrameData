import { defineConfig, ProxyOptions } from "vite";
import react from "@vitejs/plugin-react-swc";
import plugin from "@vitejs/plugin-react";
import fs from "fs";
import path from "path";
import child_process from "child_process";
import { env } from "process";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), plugin()],
  server: {
    host: true,
	port: 3000
  },
  build: {
    outDir: "dist",
  },
});
