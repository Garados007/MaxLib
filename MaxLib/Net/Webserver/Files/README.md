# Files Content Provider

## Hierarchy

*original hierarchy in source file*

- [File System Service](FileSystemService.cs)
- Content Provider
	- [Content Provider](ContentProvider.cs)
	- Content Grabber
		- [Content Environment](ContentEnvironment.cs)
		- Content Source
			- [Content Source](ContentSource.cs)
			- [IO Content Source](IOContentSource.cs)
			- [Virtual Content Source](VirtualContentSource.cs)
		- Content Info
			- [Content Info](ContentInfo.cs)
			- [Content Type](CotnentType.cs)
			- [Directory Info](DirectoryInfo.cs)
			- [File Info](FileInfo.cs)
			- System.IO Implementation (direct Access)
				- [IO Directory Info](IODirectoryInfo.cs)
				- [IO File Info](IOFileInfo.cs)
		- Icons
			- [Icon Fetcher](IconFetcher.cs)
			- [Icon Factory](IconFactory.cs)
			- [Icon Info](IconInfo.cs)
	- Content Viewer
		- [Content Result](ContentResult.cs)
		- [Content Viewer](ContentViewer.cs)
		- [Content Info Viewer](ContentInfoViewer.cs)
		- [Content Viewer Factory](ContentViewerFactory.cs)
- Source Provider
	- [Source Provider](SourceProvider.cs)
	- [Source Provider Factory](SourceProviderFactory.cs)
	- [Ressource Token](RessourceToken.cs)
