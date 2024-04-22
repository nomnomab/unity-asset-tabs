# Asset Tabs

This package allows for you to easily make docked tabs for your assets. Simply drag the object into an EditorWindow's tab row, and voila!

https://github.com/nomnomab/asset-tabs/assets/28305689/f9f3c806-26fd-442c-80b2-5587e983d6a7

- [How to use](#how-to-use)
- [Install](#install)
    - [via Package Manager](#via-package-manager)
    - [via Git URL](#via-git-url)
- [Configuration](#configuration)

<!-- toc -->

## How to use

There are many ways to use this:

- Right click an asset and select `Open as Tab` to open a new tab in the focused window.
- Drag an asset, or a scene object, into the tab row of any window to dock it
- Drag an asset from a folder tab into the tab row to dock it

## Install

### via Package Manager

1. Open the package manager from `Window > Package Manager`
2. Click the plus at the top left of the window
3. Select `Add package from git URL...`
4. Paste in `https://github.com/nomnomab/asset-tabs.git`
5. Press the add button, or press enter

### via Git URL

1. Open `Packages/manifest.json` with your favorite text editor. 
2. Add following line to the dependencies block:

```json
{
  "dependencies": {
    "com.nomnom.asset-tabs": "https://github.com/nomnomab/asset-tabs.git"
  }
}
```
