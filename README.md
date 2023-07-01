# Story Manager
Windows desktop program for downloading, organizing, and viewing your favorite stories from [Literotica](https://literotica.com/stories/). Free and open-source, available under [MIT license](https://github.com/git/git-scm.com/blob/main/MIT-LICENSE.txt).

## Features

- Download and save stories for offline viewing

  ![Story Downloader1](https://github.com/Videogamers0/StoryManager/assets/9426230/79fb0be0-88e6-42bd-874b-e9cb034b612a)

  Paste a link into the textbox and click **Download** to retrieve a single story. Or paste a link to an author's submissions page and click **Retrieve stories by Author** to batch-download many   stories by the same author.

  ![Story Downloader2](https://github.com/Videogamers0/StoryManager/assets/9426230/66139d16-cae8-4e5e-b8e3-f824a9fda2a7)

  *StoryManager* automatically retrieves all pages of all chapters when you download a story, and saves the story content to a single combined file.

- Built-in viewer

  Downloaded stories are saved as a .html file so you can read them by opening the .html file with your favorite browser. Or, if you prefer, view the story directly within *StoryManager*.

  ![MainWindow1](https://github.com/Videogamers0/StoryManager/assets/9426230/cc173dd5-fb41-4853-958a-79ff47ac786c)

  The built-in viewer has a handful of minor usability features such as settings to change the font size, text color, or background color. Highlight a comma-separated list of your favorite keywords. Jump to specific pages or chapters. *StoryManager* also attempts to save your scroll position so you can re-visit a story later from where you left off (although the scroll position is dependent on the width of the viewer, so if you resize the window it won't be retained)

- All your stories in one convenient place

  The sidebar on the left contains all the stories you've downloaded, grouped by the author. Click to select one and load it. Customize what data appears from the filtering dropdown.

  ![Stories1](https://github.com/Videogamers0/StoryManager/assets/9426230/67a4b8f9-9488-470a-90a8-e3afea2756b0)

- Keep it organized

  Rate stories from 1 to 5 stars, mark as read or unread, add or remove to favorites, add your own notes to remember what a story was about, filter the list based on various criteria like word count, download date, rating, etc.

- Built-in search

  Search for stories from directly within *StoryManager* by expanding the search settings at the bottom of the window.

  ![Search1](https://github.com/Videogamers0/StoryManager/assets/9426230/527e2f36-dd3a-4b4b-aed6-e1fcac41fcdd)

## FAQS

- Where are the stories saved?
  - *%MyDocuments%\StoryManager\Stories\Literotica\{AuthorName}\{StoryTitle}*
    - This is usually something like *"C:\Users\{Username}\Documents\StoryManager\Stories\Literotica\"*
  - Stories are saved as both a .html file and as a .txt file. Some stories are better when viewed from the .html files.
  - You can change the default directory from the settings dialog (accessed from a button in the top-left corner of the program window)
- Where are the program's settings saved?
  - *%MyDocuments%\StoryManager\settings.json*
- Does it work with illustrated stories?
  - Yes, but images are currently stored as links instead of saved to local files. If you open an illustrated story, the images will be retrieved from literotica's servers, requiring an internet connection. Maybe I'll fix this limitation at some point if anyone actually cares enough.
