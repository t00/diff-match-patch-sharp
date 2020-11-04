# DiffMatchPatchSharp
DiffMatchPatch by Google netstandard2.0 build with XML, HTML and plain text diff support

HTML diff example:

    var html1 = "<p><span>First text in the paragraph.</span><span>More text added later</span>Hello</p>";
    var html2 = "<p><span>First text </span><span>in the paragraph. A new text inserted.</span><span>More text coming later.</span>Hell</p>";
    var dc = new HtmlTextDiffChanges { AddedColor = Color.Aqua };
    var diff = dc.CompareHtml(html1, html2);
    // diff == "<p><span>First text </span><span>in the paragraph.<span style=\"background-color: #9ACD32\"> A new text inserted.</span></span><span>More text <span style=\"background-color: #FFFF00\">coming</span> later<span style=\"background-color: #9ACD32\">.</span></span>Hell</p>";

More examples in unit tests - library can process plain text, XML or HTML with custom logic to mark or list changes
