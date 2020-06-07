# Files Content Provider

## Hierarchy

*original hierarchy in source file*

- [`ContentProvider` **:** `FileSystemService`](ContentProvider.cs) 
	
	*provides the content, the icons and shows them*
- [abstract `FileSystemService` **:** `WebService`](FileSystemService.cs) 

	*base for all services that provides file content with `ContentEnvironment`*
- **Content Provider**
	- **Content Grabber**
		- [`ContentEnvironment`](ContentEnvironment.cs) 

			*contains multiple `ContentSource`, provide the contents of all of them*
		- **Content Source**
			- [abstract `ContentSource`](Content/Grabber/Source/ContentSource.cs) 
			
				*source that can provide contents for a ressource string*
			- [`IOContentSource` **:** `ContentSource`](Content/Grabber/Source/IOContentSource.cs) 
			
				*provide local contents*
			- [`VirtualContentSource` **:** `ContentSource`](Content/Grabber/Source/VirtualContentSource.cs) 
			
				*provides virtual directories*
		- **Content Info**
			- [abstract `ContentInfo`](Content/Grabber/Info/ContentInfo.cs) 
			
				*basic info about a single content*
			- [enum `ContentType`](Content/Grabber/Info/CotnentType.cs) 
			
				*content type*
			- [abstract `DirectoryInfo` **:** `ContentInfo`](Content/Grabber/Info/DirectoryInfo.cs) 
			
				*basic info about a directory*
			- [astract `FileInfo` **:** `ContentInfo`](Content/Grabber/Info/FileInfo.cs) 
			
				*basic info about a file*
			- **System.IO Implementation (direct Access)**
				- [`IODirectoryInfo` **:** `DirectoryInfo`](Content/Grabber/Info/IODirectoryInfo.cs) 
				
					*info about a local directory*
				- [`IOFileInfo` **:** `FileInfo`](Content/Grabber/Info/IOFileInfo.cs) 
				
					*info about a local file*
		- **Icons**
			- [abstract `IconFetcher`](Content/Grabber/Icons/IconFetcher.cs) 
			
				*base for icon fetcher or combiner*
			- [`IconFactory`](Content/Grabber/Icons/IconFactory.cs) 
			
				*combines and creates `IconFetcher`*
			- [`IconInfo`](Content/Grabber/Icons/IconInfo.cs) 
			
				*info about an icon ressource*
	- **Content Viewer**
		- [`ContentResult`](Content/Viewer/ContentResult.cs) 
		
			*combines multiple `ContentInfo` as a single result*
		- [abstract `ContentViewer`](Content/Viewer/ContentViewer.cs) 
		
			*view the whole `ContentResult`*
		- [abstract `ContentInfoViewer`](Content/Viewer/ContentInfoViewer.cs) 
		
			*view a single `ContentInfo`*
		- [`ContentViewerFactory`](Content/Viewer/ContentViewerFactory.cs) 
		
			*generate `ContentViewer`*
- **Source Provider**
	- [abstract `SourceProvider`](Source/SourceProvider.cs) 
	
		*provides the content of ressource identified by `RessourceToken`*
	- [Source ProviderFactory](Source/SourceProviderFactory.cs) 
	
		*generate `SourceProvider`*
	- [abstract `RessourceToken`](Source/RessourceToken.cs) 
	
		*a token to identify a ressource later*
