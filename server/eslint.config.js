// ESLint flat config: JS + TypeScript recommended rules (see planning/still-water/decisions/14-server-test-tooling.md).
import js from "@eslint/js";
import tseslint from "typescript-eslint";

export default tseslint.config(
  { ignores: ["build/", "node_modules/"] },
  js.configs.recommended,
  tseslint.configs.recommended,
);
