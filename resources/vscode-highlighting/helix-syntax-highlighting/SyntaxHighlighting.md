To install the extension:
- Open VSCode.
- Go to the Extensions view by clicking on the square icon on the sidebar or pressing Ctrl+Shift+X.
- Click on the ... (More Actions) button at the top of the Extensions view, select Install from VSIX..., and choose the .vsix file you just created.
- After installing, files with the .helix extension should now have syntax highlighting based on the rules defined in helix.tmLanguage.

To save changes to the extension:
- Make any changes to the helix.tmLanguage file and update package.json
- `npm install -g vsce` if you don't have it already
- open the helix-syntax-highlighting directory in terminal
- run `vsce package` to generate a new `.vsix` file