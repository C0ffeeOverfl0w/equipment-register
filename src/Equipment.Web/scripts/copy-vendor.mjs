import { copyFile, cp, mkdir } from "node:fs/promises";
import { createRequire } from "node:module";
import path from "node:path";
import { fileURLToPath } from "node:url";

const require = createRequire(import.meta.url);
const projectRoot = fileURLToPath(new URL("../", import.meta.url));
const vendorRoot = path.join(projectRoot, "wwwroot", "vendor");
const jqueryRoot = path.dirname(require.resolve("jquery/package.json"));
const devExtremeRoot = path.dirname(require.resolve("devextreme-dist/package.json"));

await mkdir(path.join(vendorRoot, "jquery"), { recursive: true });
await mkdir(path.join(vendorRoot, "devextreme", "js", "localization"), { recursive: true });
await copyFile(path.join(jqueryRoot, "dist", "jquery.min.js"), path.join(vendorRoot, "jquery", "jquery.min.js"));
await copyFile(path.join(devExtremeRoot, "js", "dx.all.js"), path.join(vendorRoot, "devextreme", "js", "dx.all.js"));
await copyFile(path.join(devExtremeRoot, "js", "localization", "dx.messages.ru.js"),
    path.join(vendorRoot, "devextreme", "js", "localization", "dx.messages.ru.js"));
await cp(path.join(devExtremeRoot, "css"), path.join(vendorRoot, "devextreme", "css"), { recursive: true });
console.log("Pinned jQuery and DevExtreme assets copied to wwwroot/vendor.");
