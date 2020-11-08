# DiffMatchPatchSharp
DiffMatchPatch by Google with XML, HTML and plain text diff support, side by side (netstandard2.0)

Package contains plain DiffMatchPatch library with C# naming style and also several extensions allowing parsing of structured formats.
Custom formatters can be added into pipeline to present the changes in a customized way.

 - TextDiffChanges - for finding plain text differences
 - XmlTextDiffChanges - for comparing XML documents
 - FlowDocumentDiffChanges - for FlowDocument comparison (only available in sources, no nuget package yet)
 - HtmlTextDiffChanges - for HTML comparison.

Main purpose of the library is HTML comparison.
Usually diff shows results in a single document which includes all changes: deleted, modified, added.
This library produces side by side HTML diff results in 3 colors - red (deleted), yellow (changed) and green (added) - all configurable.
"Before" document changes are coloured with deleted and changed markings while "After" document changes are coloured with changed and added markings.

HTML diff example:

    var html1 = "<p><span>First text in the paragraph.</span><span>More text added later</span>Hello</p>";
    var html2 = "<p><span>First text </span><span>in the paragraph. A new text inserted.</span><span>More text coming later.</span>Hell</p>";
    var dc = new HtmlTextDiffChanges { AddedColor = Color.Aqua };
    var (removed, added) = dc.CompareHtml(html1, html2);
    Assert.AreEqual("<p><span>First text in the paragraph.</span><span>More text <span style=\"background-color: #FFFF00\">added</span> later</span>Hell<span style=\"background-color: #FF6347\">o</span></p>", removed);
    Assert.AreEqual("<p><span>First text </span><span>in the paragraph.<span style=\"background-color: #00FFFF\"> A new text inserted.</span></span><span>More text <span style=\"background-color: #FFFF00\">coming</span> later<span style=\"background-color: #00FFFF\">.</span></span>Hell</p>", added);

More examples are available in unit tests.

