// Blocks edits to core systems owned by Oak.
// Unlock for your own work with:  ALLOW_CORE_EDIT=1
let raw = "";
process.stdin.on("data", (c) => (raw += c));
process.stdin.on("end", () => {
  if (process.env.ALLOW_CORE_EDIT === "1") process.exit(0);

  let filePath = "";
  try {
    filePath = JSON.parse(raw)?.tool_input?.file_path || "";
  } catch {
    process.exit(0);
  }
  if (!filePath) process.exit(0);

  const normalized = filePath.replace(/\\/g, "/");
  const protectedDirs = [
    "Scripts/Core/",
    "Scripts/Player/",
    "Scripts/Interaction/",
    "Scripts/Save/",
    "Scripts/Items/",
    "Scripts/Dialogue/",
    "Scripts/Settings/",
    "Scripts/Camera/",
  ];

  if (protectedDirs.some((d) => normalized.includes(d))) {
    console.error(`BLOCKED: ${filePath} is a core system owned by Oak.`);
    console.error(
      "Do not edit it. Stop and output a REQUEST TO SENIOR block explaining what you need and why."
    );
    process.exit(2);
  }
  process.exit(0);
});