// BlazorHeadless — JS initializer.
//
// Blazor automatically loads `{AssemblyName}.lib.module.js` from every Razor
// class library and calls these hooks at startup. Importing the runtime here
// installs the global, declarative behaviours ([data-bh-keys] key handling,
// dismissable layers, interaction attributes) before any component renders,
// so consumers never need a <script> tag.

let runtime;

async function load() {
    runtime ??= import('./blazor-headless.js').then((m) => { m.install(); return m; });
    return runtime;
}

// Blazor Web App (.NET 8+)
export function afterWebStarted() { return load(); }

// Blazor Server / WebAssembly standalone
export function afterStarted() { return load(); }
