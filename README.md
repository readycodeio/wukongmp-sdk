# Wukong C# Mod

## Deployment

### Building

- **Production:** `./BuildModZip Release`
- **Staging:** `./BuildModZip Debug`

This creates a `.7z` archive with the version number in the filename.

### Uploading

Upload the `.7z` file to Azure Blob Storage:
- Storage account: `readymstorage`
- Container: `wukong-mp-mod`
- Folder: `mod`

### Updating Game Server

Copy the version number to the gameserver repository: https://github.com/readycodeio/readym-gameserver

Update the `modVersion` label in the appropriate file:
- **Production:** `fleet.yml`
- **Staging:** `fleet-staging.yml`