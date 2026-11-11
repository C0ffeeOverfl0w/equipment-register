import { readdir } from "node:fs/promises";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const sourceDirectory = new URL("../wwwroot/js/", import.meta.url);
for (const name of await readdir(sourceDirectory)) {
    if (name.endsWith(".js")) {
        execFileSync(process.execPath, ["--check", fileURLToPath(new URL(name, sourceDirectory))], { stdio: "inherit" });
    }
}
console.log("Client JavaScript syntax checks passed.");
