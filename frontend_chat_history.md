Thanks for reaching out to the backend developer!

However, there is a huge misunderstanding from the backend's side. The backend developer looked at your "Debug JSON" message (which is what the backend returns to MAUI when we fetch the note) and mistakenly assumed that MAUI is sending contentJson:"" in the POST request.

I captured the EXACT raw JSON payload that MAUI is sending to the backend right now in payload.txt when you hit Save. Here is the exact JSON MAUI is currently sending:

json
{
  "title": "gjgjgj",
  "summary": "",
  "tags": [],
  "categoryId": null,
  "toolName": null,
  "isPinned": false,
  "isFavorite": false,
  "isArchived": false,
  "shareWithEveryone": false,
  "content": {
    "version": 2,
    "blocks": []
  }
}
As you can see, MAUI IS sending an object named "content" and it IS NOT sending a string! It's sending exactly what the backend developer asked for.

Yet, when MAUI sends this payload, the backend API crashes with a 500 Internal Server Error. Because the backend crashes, it refuses to save the note, which is why your blocks vanish!

The Real Issue
The backend developer's example expects the blocks array to look like this:

json
{
  "id": "abc-123",
  "type": "heading",
  "level": 1,
  "text": "My Heading"
}
But the existing MAUI frontend has been built to expect and send blocks like this:

json
{
  "id": "abc-123",
  "type": 0,
  "content": "My Heading"
}
(MAUI uses an integer 0 for Heading, and a single string content property for the text, whereas the backend expects type to be a string "heading" and the text to be in a property called "text", plus a "level" property).

How to Fix This
We have two options, and I can implement either one instantly:

Option 1 (Recommended): You tell the backend developer that MAUI is already sending "content", and they need to check their server logs to see why their backend throws a 500 Internal Server Error even when MAUI sends an empty blocks array ("blocks": []). Once they fix the 500 error, I will write a quick translation layer in MAUI to convert our UI blocks (type: 0) into their backend format (type: "heading") before sending it.

Option 2: I can go ahead and write the translation layer right now so MAUI sends the exact JSON schema they asked for. However, because the backend is currently crashing even when blocks is completely empty, translating the blocks might not fix the 500 crash if the backend is rejecting something else (like "shareWithEveryone": false).