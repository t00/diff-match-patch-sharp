﻿/*
 * Copyright 2008 Google Inc. All Rights Reserved.
 * Author: fraser@google.com (Neil Fraser)
 * Author: anteru@developer.shelter13.net (Matthaeus G. Chajdas)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Diff Match and Patch
 * http://code.google.com/p/google-diff-match-patch/
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace DiffMatchPatchSharp;

internal static class CompatibilityExtensions
{
    // JScript splice function
    public static List<T> Splice<T>(this List<T> input, int start, int count,
        params T[] objects)
    {
        List<T> deletedRange = input.GetRange(start, count);
        input.RemoveRange(start, count);
        input.InsertRange(start, objects);

        return deletedRange;
    }

    // Java substring function
    public static string JavaSubstring(this string s, int begin, int end)
    {
        return s.Substring(begin, end - begin);
    }
}

/**-
 * The data structure representing a diff is a List of Diff objects:
 * {Diff(Operation.DELETE, "Hello"), Diff(Operation.INSERT, "Goodbye"),
 *  Diff(Operation.EQUAL, " world.")}
 * which means: delete "Hello", add "Goodbye" and keep " world."
 */
public enum Operation
{
    Delete, Insert, Equal
}


/**
 * Class representing one diff operation.
 */
public class Diff
{
    /// <summary>
    /// One of: Insert, Delete or Equal.
    /// </summary>
    public Operation Operation { get; set; }

    /// <summary>
    /// The text associated with this diff operation.
    /// </summary>
    public string Text { get; set; }

    /**
     * Constructor.  Initializes the diff with the provided values.
     * @param operation One of INSERT, DELETE or EQUAL.
     * @param text The text being applied.
     */
    public Diff(Operation operation, string text)
    {
        // Construct a diff with the specified operation and text.
        this.Operation = operation;
        this.Text = text;
    }

    /**
     * Display a human-readable version of this Diff.
     * @return text version.
     */
    public override string ToString()
    {
        string prettyText = this.Text.Replace('\n', '\u00b6');
        return "Diff(" + this.Operation + ",\"" + prettyText + "\")";
    }

    /**
     * Is this Diff equivalent to another Diff?
     * @param d Another Diff to compare against.
     * @return true or false.
     */
    public override bool Equals(object obj)
    {
        // If parameter is null return false.
        if (obj == null)
        {
            return false;
        }

        // If parameter cannot be cast to Diff return false.
        Diff p = obj as Diff;
        if (p == null)
        {
            return false;
        }

        // Return true if the fields match.
        return p.Operation == this.Operation && p.Text == this.Text;
    }

    public bool Equals(Diff obj)
    {
        // If parameter is null return false.
        if (obj == null)
        {
            return false;
        }

        // Return true if the fields match.
        return obj.Operation == this.Operation && obj.Text == this.Text;
    }

    public override int GetHashCode()
    {
        return Text.GetHashCode() ^ Operation.GetHashCode();
    }
}


/**
 * Class representing one patch operation.
 */
public class Patch
{
    public List<Diff> Diffs { get; set; } = new List<Diff>();

    public int Start1 { get; set; }

    public int Start2 { get; set; }

    public int Length1 { get; set; }

    public int Length2 { get; set; }

    /**
     * Emmulate GNU diff's format.
     * Header: @@ -382,8 +481,9 @@
     * Indicies are printed as 1-based, not 0-based.
     * @return The GNU diff string.
     */
    public override string ToString()
    {
        string coords1, coords2;
        if (this.Length1 == 0)
        {
            coords1 = this.Start1 + ",0";
        }
        else if (this.Length1 == 1)
        {
            coords1 = Convert.ToString(this.Start1 + 1);
        }
        else
        {
            coords1 = (this.Start1 + 1) + "," + this.Length1;
        }
        if (this.Length2 == 0)
        {
            coords2 = this.Start2 + ",0";
        }
        else if (this.Length2 == 1)
        {
            coords2 = Convert.ToString(this.Start2 + 1);
        }
        else
        {
            coords2 = (this.Start2 + 1) + "," + this.Length2;
        }
        StringBuilder text = new StringBuilder();
        text.Append("@@ -").Append(coords1).Append(" +").Append(coords2)
            .Append(" @@\n");
        // Escape the body of the patch with %xx notation.
        foreach (Diff aDiff in this.Diffs)
        {
            switch (aDiff.Operation)
            {
                case Operation.Insert:
                    text.Append('+');
                    break;
                case Operation.Delete:
                    text.Append('-');
                    break;
                case Operation.Equal:
                    text.Append(' ');
                    break;
            }

            text.Append(HttpUtility.UrlEncode(aDiff.Text,
                new UTF8Encoding()).Replace('+', ' ')).Append("\n");
        }

        return DiffMatchPatch.UnescapeForEncodeUriCompatability(
            text.ToString());
    }
}


/// <summary>
/// Class containing the diff, match and patch methods.
/// Also Contains the behaviour settings.
/// </summary>
public class DiffMatchPatch
{
    // Defaults.
    // Set these on your diff_match_patch instance to override the defaults.

    /// <summary>
    /// Number of seconds to map a diff before giving up (0 for infinity).
    /// </summary>
    public float DiffTimeout { get; set; } = 0.0f;

    /// <summary>
    /// Cost of an empty edit operation in terms of edit characters.
    /// </summary>
    public short DiffEditCost { get; set; } = 4;

    /// <summary>
    /// At what point is no match declared (0.0 = perfection, 1.0 = very loose).
    /// </summary>
    public float MatchThreshold { get; set; } = 0.5f;

    /// <summary>
    /// How far to search for a match (0 = exact location, 1000+ = broad match).
    /// A match this many characters away from the expected location will add
    /// 1.0 to the score (0.0 is a perfect match).
    /// </summary>
    public int MatchDistance { get; set; } = 1000;

    /// <summary>
    /// When deleting a large block of text (over ~64 characters), how close
    /// do the contents have to be to match the expected contents. (0.0 =
    /// perfection, 1.0 = very loose).  Note that Match_Threshold controls
    /// how closely the end points of a delete need to match.
    /// </summary>
    public float PatchDeleteThreshold { get; set; } = 0.5f;

    /// <summary>
    /// Chunk size for context length.
    /// </summary>
    public short PatchMargin { get; set; } = 4;

    /// <summary>
    /// The number of bits in an int.
    /// </summary>
    private short MatchMaxBits = 32;

    //  DIFF FUNCTIONS

    /**
     * Find the differences between two texts.
     * Run a faster, slightly less optimal diff.
     * This method allows the 'checklines' of diff_main() to be optional.
     * Most of the time checklines is wanted, so default to true.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @return List of Diff objects.
     */
    public List<Diff> DiffMain(string text1, string text2)
    {
        return DiffMain(text1, text2, true);
    }

    /**
     * Find the differences between two texts.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param checklines Speedup flag.  If false, then don't run a
     *     line-level diff first to identify the changed areas.
     *     If true, then run a faster slightly less optimal diff.
     * @return List of Diff objects.
     */
    public List<Diff> DiffMain(string text1, string text2, bool checklines)
    {
        // Set a deadline by which time the diff must be complete.
        DateTime deadline;
        if (this.DiffTimeout <= 0)
        {
            deadline = DateTime.MaxValue;
        }
        else
        {
            deadline = DateTime.Now +
                       new TimeSpan(((long)(DiffTimeout * 1000)) * 10000);
        }
        return DiffMain(text1, text2, checklines, deadline);
    }

    /**
     * Find the differences between two texts.  Simplifies the problem by
     * stripping any common prefix or suffix off the texts before diffing.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param checklines Speedup flag.  If false, then don't run a
     *     line-level diff first to identify the changed areas.
     *     If true, then run a faster slightly less optimal diff.
     * @param deadline Time when the diff should be complete by.  Used
     *     internally for recursive calls.  Users should set DiffTimeout
     *     instead.
     * @return List of Diff objects.
     */
    private List<Diff> DiffMain(string text1, string text2, bool checklines,
        DateTime deadline)
    {
        // Check for null inputs not needed since null can't be passed in C#.

        // Check for equality (speedup).
        List<Diff> diffs;
        if (text1 == text2)
        {
            diffs = new List<Diff>();
            if (text1.Length != 0)
            {
                diffs.Add(new Diff(Operation.Equal, text1));
            }
            return diffs;
        }

        // Trim off common prefix (speedup).
        int commonlength = DiffCommonPrefix(text1, text2);
        string commonprefix = text1.Substring(0, commonlength);
        text1 = text1.Substring(commonlength);
        text2 = text2.Substring(commonlength);

        // Trim off common suffix (speedup).
        commonlength = DiffCommonSuffix(text1, text2);
        string commonsuffix = text1.Substring(text1.Length - commonlength);
        text1 = text1.Substring(0, text1.Length - commonlength);
        text2 = text2.Substring(0, text2.Length - commonlength);

        // Compute the diff on the middle block.
        diffs = DiffCompute(text1, text2, checklines, deadline);

        // Restore the prefix and suffix.
        if (commonprefix.Length != 0)
        {
            diffs.Insert(0, (new Diff(Operation.Equal, commonprefix)));
        }
        if (commonsuffix.Length != 0)
        {
            diffs.Add(new Diff(Operation.Equal, commonsuffix));
        }

        DiffCleanupMerge(diffs);
        return diffs;
    }

    /**
     * Find the differences between two texts.  Assumes that the texts do not
     * have any common prefix or suffix.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param checklines Speedup flag.  If false, then don't run a
     *     line-level diff first to identify the changed areas.
     *     If true, then run a faster slightly less optimal diff.
     * @param deadline Time when the diff should be complete by.
     * @return List of Diff objects.
     */
    private List<Diff> DiffCompute(string text1, string text2, bool checklines, DateTime deadline)
    {
        List<Diff> diffs = new List<Diff>();

        if (text1.Length == 0)
        {
            // Just add some text (speedup).
            diffs.Add(new Diff(Operation.Insert, text2));
            return diffs;
        }

        if (text2.Length == 0)
        {
            // Just delete some text (speedup).
            diffs.Add(new Diff(Operation.Delete, text1));
            return diffs;
        }

        string longtext = text1.Length > text2.Length ? text1 : text2;
        string shorttext = text1.Length > text2.Length ? text2 : text1;
        int i = longtext.IndexOf(shorttext, StringComparison.Ordinal);
        if (i != -1)
        {
            // Shorter text is inside the longer text (speedup).
            Operation op = (text1.Length > text2.Length) ?
                Operation.Delete : Operation.Insert;
            diffs.Add(new Diff(op, longtext.Substring(0, i)));
            diffs.Add(new Diff(Operation.Equal, shorttext));
            diffs.Add(new Diff(op, longtext.Substring(i + shorttext.Length)));
            return diffs;
        }

        if (shorttext.Length == 1)
        {
            // Single character string.
            // After the previous speedup, the character can't be an equality.
            diffs.Add(new Diff(Operation.Delete, text1));
            diffs.Add(new Diff(Operation.Insert, text2));
            return diffs;
        }

        // Check to see if the problem can be split in two.
        string[] hm = DiffHalfMatch(text1, text2);
        if (hm != null)
        {
            // A half-match was found, sort out the return data.
            string text1_a = hm[0];
            string text1_b = hm[1];
            string text2_a = hm[2];
            string text2_b = hm[3];
            string mid_common = hm[4];
            // Send both pairs off for separate processing.
            List<Diff> diffs_a = DiffMain(text1_a, text2_a, checklines, deadline);
            List<Diff> diffs_b = DiffMain(text1_b, text2_b, checklines, deadline);
            // Merge the results.
            diffs = diffs_a;
            diffs.Add(new Diff(Operation.Equal, mid_common));
            diffs.AddRange(diffs_b);
            return diffs;
        }

        if (checklines && text1.Length > 100 && text2.Length > 100)
        {
            return DiffLineMode(text1, text2, deadline);
        }

        return DiffBisect(text1, text2, deadline);
    }

    /**
     * Do a quick line-level diff on both strings, then rediff the parts for
     * greater accuracy.
     * This speedup can produce non-minimal diffs.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param deadline Time when the diff should be complete by.
     * @return List of Diff objects.
     */
    private List<Diff> DiffLineMode(string text1, string text2,
        DateTime deadline)
    {
        // Scan the text on a line-by-line basis first.
        Object[] b = DiffLinesToChars(text1, text2);
        text1 = (string)b[0];
        text2 = (string)b[1];
        List<string> linearray = (List<string>)b[2];

        List<Diff> diffs = DiffMain(text1, text2, false, deadline);

        // Convert the diff back to original text.
        DiffCharsToLines(diffs, linearray);
        // Eliminate freak matches (e.g. blank lines)
        DiffCleanupSemantic(diffs);

        // Rediff any replacement blocks, this time character-by-character.
        // Add a dummy entry at the end.
        diffs.Add(new Diff(Operation.Equal, string.Empty));
        int pointer = 0;
        int count_delete = 0;
        int count_insert = 0;
        string text_delete = string.Empty;
        string text_insert = string.Empty;
        while (pointer < diffs.Count)
        {
            switch (diffs[pointer].Operation)
            {
                case Operation.Insert:
                    count_insert++;
                    text_insert += diffs[pointer].Text;
                    break;
                case Operation.Delete:
                    count_delete++;
                    text_delete += diffs[pointer].Text;
                    break;
                case Operation.Equal:
                    // Upon reaching an equality, check for prior redundancies.
                    if (count_delete >= 1 && count_insert >= 1)
                    {
                        // Delete the offending records and add the merged ones.
                        diffs.RemoveRange(pointer - count_delete - count_insert,
                            count_delete + count_insert);
                        pointer = pointer - count_delete - count_insert;
                        List<Diff> a =
                            this.DiffMain(text_delete, text_insert, false, deadline);
                        diffs.InsertRange(pointer, a);
                        pointer = pointer + a.Count;
                    }
                    count_insert = 0;
                    count_delete = 0;
                    text_delete = string.Empty;
                    text_insert = string.Empty;
                    break;
            }
            pointer++;
        }
        diffs.RemoveAt(diffs.Count - 1);  // Remove the dummy entry at the end.

        return diffs;
    }

    /**
     * Find the 'middle snake' of a diff, split the problem in two
     * and return the recursively constructed diff.
     * See Myers 1986 paper: An O(ND) Difference Algorithm and Its Variations.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param deadline Time at which to bail if not yet complete.
     * @return List of Diff objects.
     */
    protected List<Diff> DiffBisect(string text1, string text2, DateTime deadline)
    {
        // Cache the text lengths to prevent multiple calls.
        int text1_length = text1.Length;
        int text2_length = text2.Length;
        int max_d = (text1_length + text2_length + 1) / 2;
        int v_offset = max_d;
        int v_length = 2 * max_d;
        int[] v1 = new int[v_length];
        int[] v2 = new int[v_length];
        for (int x = 0; x < v_length; x++)
        {
            v1[x] = -1;
            v2[x] = -1;
        }
        v1[v_offset + 1] = 0;
        v2[v_offset + 1] = 0;
        int delta = text1_length - text2_length;
        // If the total number of characters is odd, then the front path will
        // collide with the reverse path.
        bool front = (delta % 2 != 0);
        // Offsets for start and end of k loop.
        // Prevents mapping of space beyond the grid.
        int k1start = 0;
        int k1end = 0;
        int k2start = 0;
        int k2end = 0;
        for (int d = 0; d < max_d; d++)
        {
            // Bail out if deadline is reached.
            if (DateTime.Now > deadline)
            {
                break;
            }

            // Walk the front path one step.
            for (int k1 = -d + k1start; k1 <= d - k1end; k1 += 2)
            {
                int k1_offset = v_offset + k1;
                int x1;
                if (k1 == -d || k1 != d && v1[k1_offset - 1] < v1[k1_offset + 1])
                {
                    x1 = v1[k1_offset + 1];
                }
                else
                {
                    x1 = v1[k1_offset - 1] + 1;
                }
                int y1 = x1 - k1;
                while (x1 < text1_length && y1 < text2_length
                                         && text1[x1] == text2[y1])
                {
                    x1++;
                    y1++;
                }
                v1[k1_offset] = x1;
                if (x1 > text1_length)
                {
                    // Ran off the right of the graph.
                    k1end += 2;
                }
                else if (y1 > text2_length)
                {
                    // Ran off the bottom of the graph.
                    k1start += 2;
                }
                else if (front)
                {
                    int k2_offset = v_offset + delta - k1;
                    if (k2_offset >= 0 && k2_offset < v_length && v2[k2_offset] != -1)
                    {
                        // Mirror x2 onto top-left coordinate system.
                        int x2 = text1_length - v2[k2_offset];
                        if (x1 >= x2)
                        {
                            // Overlap detected.
                            return DiffBisectSplit(text1, text2, x1, y1, deadline);
                        }
                    }
                }
            }

            // Walk the reverse path one step.
            for (int k2 = -d + k2start; k2 <= d - k2end; k2 += 2)
            {
                int k2_offset = v_offset + k2;
                int x2;
                if (k2 == -d || k2 != d && v2[k2_offset - 1] < v2[k2_offset + 1])
                {
                    x2 = v2[k2_offset + 1];
                }
                else
                {
                    x2 = v2[k2_offset - 1] + 1;
                }
                int y2 = x2 - k2;
                while (x2 < text1_length && y2 < text2_length
                                         && text1[text1_length - x2 - 1]
                                         == text2[text2_length - y2 - 1])
                {
                    x2++;
                    y2++;
                }
                v2[k2_offset] = x2;
                if (x2 > text1_length)
                {
                    // Ran off the left of the graph.
                    k2end += 2;
                }
                else if (y2 > text2_length)
                {
                    // Ran off the top of the graph.
                    k2start += 2;
                }
                else if (!front)
                {
                    int k1_offset = v_offset + delta - k2;
                    if (k1_offset >= 0 && k1_offset < v_length && v1[k1_offset] != -1)
                    {
                        int x1 = v1[k1_offset];
                        int y1 = v_offset + x1 - k1_offset;
                        // Mirror x2 onto top-left coordinate system.
                        x2 = text1_length - v2[k2_offset];
                        if (x1 >= x2)
                        {
                            // Overlap detected.
                            return DiffBisectSplit(text1, text2, x1, y1, deadline);
                        }
                    }
                }
            }
        }
        // Diff took too long and hit the deadline or
        // number of diffs equals number of characters, no commonality at all.
        List<Diff> diffs = new List<Diff>();
        diffs.Add(new Diff(Operation.Delete, text1));
        diffs.Add(new Diff(Operation.Insert, text2));
        return diffs;
    }

    /**
     * Given the location of the 'middle snake', split the diff in two parts
     * and recurse.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param x Index of split point in text1.
     * @param y Index of split point in text2.
     * @param deadline Time at which to bail if not yet complete.
     * @return LinkedList of Diff objects.
     */
    private List<Diff> DiffBisectSplit(string text1, string text2, int x, int y, DateTime deadline)
    {
        string text1a = text1.Substring(0, x);
        string text2a = text2.Substring(0, y);
        string text1b = text1.Substring(x);
        string text2b = text2.Substring(y);

        // Compute both diffs serially.
        List<Diff> diffs = DiffMain(text1a, text2a, false, deadline);
        List<Diff> diffsb = DiffMain(text1b, text2b, false, deadline);

        diffs.AddRange(diffsb);
        return diffs;
    }

    /**
     * Split two texts into a list of strings.  Reduce the texts to a string of
     * hashes where each Unicode character represents one line.
     * @param text1 First string.
     * @param text2 Second string.
     * @return Three element Object array, containing the encoded text1, the
     *     encoded text2 and the List of unique strings.  The zeroth element
     *     of the List of unique strings is intentionally blank.
     */
    protected Object[] DiffLinesToChars(string text1, string text2)
    {
        List<string> lineArray = new List<string>();
        Dictionary<string, int> lineHash = new Dictionary<string, int>();
        // e.g. linearray[4] == "Hello\n"
        // e.g. linehash.get("Hello\n") == 4

        // "\x00" is a valid character, but various debuggers don't like it.
        // So we'll insert a junk entry to avoid generating a null character.
        lineArray.Add(string.Empty);

        string chars1 = DiffLinesToCharsMunge(text1, lineArray, lineHash);
        string chars2 = DiffLinesToCharsMunge(text2, lineArray, lineHash);
        return new Object[] { chars1, chars2, lineArray };
    }

    /**
     * Split a text into a list of strings.  Reduce the texts to a string of
     * hashes where each Unicode character represents one line.
     * @param text String to encode.
     * @param lineArray List of unique strings.
     * @param lineHash Map of strings to indices.
     * @return Encoded string.
     */
    private string DiffLinesToCharsMunge(string text, List<string> lineArray, Dictionary<string, int> lineHash)
    {
        int lineStart = 0;
        int lineEnd = -1;
        string line;
        StringBuilder chars = new StringBuilder();
        // Walk the text, pulling out a Substring for each line.
        // text.split('\n') would would temporarily double our memory footprint.
        // Modifying text would create many large strings to garbage collect.
        while (lineEnd < text.Length - 1)
        {
            lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd == -1)
            {
                lineEnd = text.Length - 1;
            }
            line = text.JavaSubstring(lineStart, lineEnd + 1);
            lineStart = lineEnd + 1;

            if (lineHash.ContainsKey(line))
            {
                chars.Append(((char)(int)lineHash[line]));
            }
            else
            {
                lineArray.Add(line);
                lineHash.Add(line, lineArray.Count - 1);
                chars.Append(((char)(lineArray.Count - 1)));
            }
        }
        return chars.ToString();
    }

    /**
     * Rehydrate the text in a diff from a string of line hashes to real lines
     * of text.
     * @param diffs List of Diff objects.
     * @param lineArray List of unique strings.
     */
    protected void DiffCharsToLines(ICollection<Diff> diffs, List<string> lineArray)
    {
        StringBuilder text;
        foreach (Diff diff in diffs)
        {
            text = new StringBuilder();
            for (int y = 0; y < diff.Text.Length; y++)
            {
                text.Append(lineArray[diff.Text[y]]);
            }
            diff.Text = text.ToString();
        }
    }

    /**
     * Determine the common prefix of two strings.
     * @param text1 First string.
     * @param text2 Second string.
     * @return The number of characters common to the start of each string.
     */
    public int DiffCommonPrefix(string text1, string text2)
    {
        // Performance analysis: http://neil.fraser.name/news/2007/10/09/
        int n = Math.Min(text1.Length, text2.Length);
        for (int i = 0; i < n; i++)
        {
            if (text1[i] != text2[i])
            {
                return i;
            }
        }
        return n;
    }

    /**
     * Determine the common suffix of two strings.
     * @param text1 First string.
     * @param text2 Second string.
     * @return The number of characters common to the end of each string.
     */
    public int DiffCommonSuffix(string text1, string text2)
    {
        // Performance analysis: http://neil.fraser.name/news/2007/10/09/
        int text1_length = text1.Length;
        int text2_length = text2.Length;
        int n = Math.Min(text1.Length, text2.Length);
        for (int i = 1; i <= n; i++)
        {
            if (text1[text1_length - i] != text2[text2_length - i])
            {
                return i - 1;
            }
        }
        return n;
    }

    /**
     * Determine if the suffix of one string is the prefix of another.
     * @param text1 First string.
     * @param text2 Second string.
     * @return The number of characters common to the end of the first
     *     string and the start of the second string.
     */
    protected int DiffCommonOverlap(string text1, string text2)
    {
        // Cache the text lengths to prevent multiple calls.
        int text1_length = text1.Length;
        int text2_length = text2.Length;
        // Eliminate the null case.
        if (text1_length == 0 || text2_length == 0)
        {
            return 0;
        }
        // Truncate the longer string.
        if (text1_length > text2_length)
        {
            text1 = text1.Substring(text1_length - text2_length);
        }
        else if (text1_length < text2_length)
        {
            text2 = text2.Substring(0, text1_length);
        }
        int text_length = Math.Min(text1_length, text2_length);
        // Quick check for the worst case.
        if (text1 == text2)
        {
            return text_length;
        }

        // Start by looking for a single character match
        // and increase length until no match is found.
        // Performance analysis: http://neil.fraser.name/news/2010/11/04/
        int best = 0;
        int length = 1;
        while (true)
        {
            string pattern = text1.Substring(text_length - length);
            int found = text2.IndexOf(pattern, StringComparison.Ordinal);
            if (found == -1)
            {
                return best;
            }
            length += found;
            if (found == 0 || text1.Substring(text_length - length) ==
                text2.Substring(0, length))
            {
                best = length;
                length++;
            }
        }
    }

    /**
     * Do the two texts share a Substring which is at least half the length of
     * the longer text?
     * This speedup can produce non-minimal diffs.
     * @param text1 First string.
     * @param text2 Second string.
     * @return Five element String array, containing the prefix of text1, the
     *     suffix of text1, the prefix of text2, the suffix of text2 and the
     *     common middle.  Or null if there was no match.
     */

    protected string[] DiffHalfMatch(string text1, string text2)
    {
        if (this.DiffTimeout <= 0)
        {
            // Don't risk returning a non-optimal diff if we have unlimited time.
            return null;
        }
        string longtext = text1.Length > text2.Length ? text1 : text2;
        string shorttext = text1.Length > text2.Length ? text2 : text1;
        if (longtext.Length < 4 || shorttext.Length * 2 < longtext.Length)
        {
            return null;  // Pointless.
        }

        // First check if the second quarter is the seed for a half-match.
        string[] hm1 = DiffHalfMatchI(longtext, shorttext,
            (longtext.Length + 3) / 4);
        // Check again based on the third quarter.
        string[] hm2 = DiffHalfMatchI(longtext, shorttext,
            (longtext.Length + 1) / 2);
        string[] hm;
        if (hm1 == null && hm2 == null)
        {
            return null;
        }
        else if (hm2 == null)
        {
            hm = hm1;
        }
        else if (hm1 == null)
        {
            hm = hm2;
        }
        else
        {
            // Both matched.  Select the longest.
            hm = hm1[4].Length > hm2[4].Length ? hm1 : hm2;
        }

        // A half-match was found, sort out the return data.
        if (text1.Length > text2.Length)
        {
            return hm;
            //return new string[]{hm[0], hm[1], hm[2], hm[3], hm[4]};
        }
        else
        {
            return new string[] { hm[2], hm[3], hm[0], hm[1], hm[4] };
        }
    }

    /**
     * Does a Substring of shorttext exist within longtext such that the
     * Substring is at least half the length of longtext?
     * @param longtext Longer string.
     * @param shorttext Shorter string.
     * @param i Start index of quarter length Substring within longtext.
     * @return Five element string array, containing the prefix of longtext, the
     *     suffix of longtext, the prefix of shorttext, the suffix of shorttext
     *     and the common middle.  Or null if there was no match.
     */
    private string[] DiffHalfMatchI(string longtext, string shorttext, int i)
    {
        // Start with a 1/4 length Substring at position i as a seed.
        string seed = longtext.Substring(i, longtext.Length / 4);
        int j = -1;
        string best_common = string.Empty;
        string best_longtext_a = string.Empty, best_longtext_b = string.Empty;
        string best_shorttext_a = string.Empty, best_shorttext_b = string.Empty;
        while (j < shorttext.Length && (j = shorttext.IndexOf(seed, j + 1,
                   StringComparison.Ordinal)) != -1)
        {
            int prefixLength = DiffCommonPrefix(longtext.Substring(i),
                shorttext.Substring(j));
            int suffixLength = DiffCommonSuffix(longtext.Substring(0, i),
                shorttext.Substring(0, j));
            if (best_common.Length < suffixLength + prefixLength)
            {
                best_common = shorttext.Substring(j - suffixLength, suffixLength)
                              + shorttext.Substring(j, prefixLength);
                best_longtext_a = longtext.Substring(0, i - suffixLength);
                best_longtext_b = longtext.Substring(i + prefixLength);
                best_shorttext_a = shorttext.Substring(0, j - suffixLength);
                best_shorttext_b = shorttext.Substring(j + prefixLength);
            }
        }
        if (best_common.Length * 2 >= longtext.Length)
        {
            return new string[]{best_longtext_a, best_longtext_b,
                best_shorttext_a, best_shorttext_b, best_common};
        }
        else
        {
            return null;
        }
    }

    /**
     * Reduce the number of edits by eliminating semantically trivial
     * equalities.
     * @param diffs List of Diff objects.
     */
    public void DiffCleanupSemantic(List<Diff> diffs)
    {
        bool changes = false;
        // Stack of indices where equalities are found.
        Stack<int> equalities = new Stack<int>();
        // Always equal to equalities[equalitiesLength-1][1]
        string lastequality = null;
        int pointer = 0;  // Index of current position.
        // Number of characters that changed prior to the equality.
        int length_insertions1 = 0;
        int length_deletions1 = 0;
        // Number of characters that changed after the equality.
        int length_insertions2 = 0;
        int length_deletions2 = 0;
        while (pointer < diffs.Count)
        {
            if (diffs[pointer].Operation == Operation.Equal)
            {  // Equality found.
                equalities.Push(pointer);
                length_insertions1 = length_insertions2;
                length_deletions1 = length_deletions2;
                length_insertions2 = 0;
                length_deletions2 = 0;
                lastequality = diffs[pointer].Text;
            }
            else
            {  // an insertion or deletion
                if (diffs[pointer].Operation == Operation.Insert)
                {
                    length_insertions2 += diffs[pointer].Text.Length;
                }
                else
                {
                    length_deletions2 += diffs[pointer].Text.Length;
                }
                // Eliminate an equality that is smaller or equal to the edits on both
                // sides of it.
                if (lastequality != null && (lastequality.Length
                                             <= Math.Max(length_insertions1, length_deletions1))
                                         && (lastequality.Length
                                             <= Math.Max(length_insertions2, length_deletions2)))
                {
                    // Duplicate record.
                    diffs.Insert(equalities.Peek(),
                        new Diff(Operation.Delete, lastequality));
                    // Change second copy to insert.
                    diffs[equalities.Peek() + 1].Operation = Operation.Insert;
                    // Throw away the equality we just deleted.
                    equalities.Pop();
                    if (equalities.Count > 0)
                    {
                        equalities.Pop();
                    }
                    pointer = equalities.Count > 0 ? equalities.Peek() : -1;
                    length_insertions1 = 0;  // Reset the counters.
                    length_deletions1 = 0;
                    length_insertions2 = 0;
                    length_deletions2 = 0;
                    lastequality = null;
                    changes = true;
                }
            }
            pointer++;
        }

        // Normalize the diff.
        if (changes)
        {
            DiffCleanupMerge(diffs);
        }
        DiffCleanupSemanticLossless(diffs);

        // Find any overlaps between deletions and insertions.
        // e.g: <del>abcxxx</del><ins>xxxdef</ins>
        //   -> <del>abc</del>xxx<ins>def</ins>
        // e.g: <del>xxxabc</del><ins>defxxx</ins>
        //   -> <ins>def</ins>xxx<del>abc</del>
        // Only extract an overlap if it is as big as the edit ahead or behind it.
        pointer = 1;
        while (pointer < diffs.Count)
        {
            if (diffs[pointer - 1].Operation == Operation.Delete &&
                diffs[pointer].Operation == Operation.Insert)
            {
                string deletion = diffs[pointer - 1].Text;
                string insertion = diffs[pointer].Text;
                int overlap_length1 = DiffCommonOverlap(deletion, insertion);
                int overlap_length2 = DiffCommonOverlap(insertion, deletion);
                if (overlap_length1 >= overlap_length2)
                {
                    if (overlap_length1 >= deletion.Length / 2.0 ||
                        overlap_length1 >= insertion.Length / 2.0)
                    {
                        // Overlap found.
                        // Insert an equality and trim the surrounding edits.
                        diffs.Insert(pointer, new Diff(Operation.Equal,
                            insertion.Substring(0, overlap_length1)));
                        diffs[pointer - 1].Text =
                            deletion.Substring(0, deletion.Length - overlap_length1);
                        diffs[pointer + 1].Text = insertion.Substring(overlap_length1);
                        pointer++;
                    }
                }
                else
                {
                    if (overlap_length2 >= deletion.Length / 2.0 ||
                        overlap_length2 >= insertion.Length / 2.0)
                    {
                        // Reverse overlap found.
                        // Insert an equality and swap and trim the surrounding edits.
                        diffs.Insert(pointer, new Diff(Operation.Equal,
                            deletion.Substring(0, overlap_length2)));
                        diffs[pointer - 1].Operation = Operation.Insert;
                        diffs[pointer - 1].Text =
                            insertion.Substring(0, insertion.Length - overlap_length2);
                        diffs[pointer + 1].Operation = Operation.Delete;
                        diffs[pointer + 1].Text = deletion.Substring(overlap_length2);
                        pointer++;
                    }
                }
                pointer++;
            }
            pointer++;
        }
    }

    /**
     * Look for single edits surrounded on both sides by equalities
     * which can be shifted sideways to align the edit to a word boundary.
     * e.g: The c<ins>at c</ins>ame. -> The <ins>cat </ins>came.
     * @param diffs List of Diff objects.
     */
    public void DiffCleanupSemanticLossless(List<Diff> diffs)
    {
        int pointer = 1;
        // Intentionally ignore the first and last element (don't need checking).
        while (pointer < diffs.Count - 1)
        {
            if (diffs[pointer - 1].Operation == Operation.Equal &&
                diffs[pointer + 1].Operation == Operation.Equal)
            {
                // This is a single edit surrounded by equalities.
                string equality1 = diffs[pointer - 1].Text;
                string edit = diffs[pointer].Text;
                string equality2 = diffs[pointer + 1].Text;

                // First, shift the edit as far left as possible.
                int commonOffset = this.DiffCommonSuffix(equality1, edit);
                if (commonOffset > 0)
                {
                    string commonString = edit.Substring(edit.Length - commonOffset);
                    equality1 = equality1.Substring(0, equality1.Length - commonOffset);
                    edit = commonString + edit.Substring(0, edit.Length - commonOffset);
                    equality2 = commonString + equality2;
                }

                // Second, step character by character right,
                // looking for the best fit.
                string bestEquality1 = equality1;
                string bestEdit = edit;
                string bestEquality2 = equality2;
                int bestScore = DiffCleanupSemanticScore(equality1, edit) +
                                DiffCleanupSemanticScore(edit, equality2);
                while (edit.Length != 0 && equality2.Length != 0
                                        && edit[0] == equality2[0])
                {
                    equality1 += edit[0];
                    edit = edit.Substring(1) + equality2[0];
                    equality2 = equality2.Substring(1);
                    int score = DiffCleanupSemanticScore(equality1, edit) +
                                DiffCleanupSemanticScore(edit, equality2);
                    // The >= encourages trailing rather than leading whitespace on
                    // edits.
                    if (score >= bestScore)
                    {
                        bestScore = score;
                        bestEquality1 = equality1;
                        bestEdit = edit;
                        bestEquality2 = equality2;
                    }
                }

                if (diffs[pointer - 1].Text != bestEquality1)
                {
                    // We have an improvement, save it back to the diff.
                    if (bestEquality1.Length != 0)
                    {
                        diffs[pointer - 1].Text = bestEquality1;
                    }
                    else
                    {
                        diffs.RemoveAt(pointer - 1);
                        pointer--;
                    }
                    diffs[pointer].Text = bestEdit;
                    if (bestEquality2.Length != 0)
                    {
                        diffs[pointer + 1].Text = bestEquality2;
                    }
                    else
                    {
                        diffs.RemoveAt(pointer + 1);
                        pointer--;
                    }
                }
            }
            pointer++;
        }
    }

    /**
     * Given two strings, comAdde a score representing whether the internal
     * boundary falls on logical boundaries.
     * Scores range from 6 (best) to 0 (worst).
     * @param one First string.
     * @param two Second string.
     * @return The score.
     */
    private int DiffCleanupSemanticScore(string one, string two)
    {
        if (one.Length == 0 || two.Length == 0)
        {
            // Edges are the best.
            return 6;
        }

        // Each port of this function behaves slightly differently due to
        // subtle differences in each language's definition of things like
        // 'whitespace'.  Since this function's purpose is largely cosmetic,
        // the choice has been made to use each language's native features
        // rather than force total conformity.
        char char1 = one[one.Length - 1];
        char char2 = two[0];
        bool nonAlphaNumeric1 = !Char.IsLetterOrDigit(char1);
        bool nonAlphaNumeric2 = !Char.IsLetterOrDigit(char2);
        bool whitespace1 = nonAlphaNumeric1 && Char.IsWhiteSpace(char1);
        bool whitespace2 = nonAlphaNumeric2 && Char.IsWhiteSpace(char2);
        bool lineBreak1 = whitespace1 && Char.IsControl(char1);
        bool lineBreak2 = whitespace2 && Char.IsControl(char2);
        bool blankLine1 = lineBreak1 && BLANKLINEEND.IsMatch(one);
        bool blankLine2 = lineBreak2 && BLANKLINESTART.IsMatch(two);

        if (blankLine1 || blankLine2)
        {
            // Five points for blank lines.
            return 5;
        }
        else if (lineBreak1 || lineBreak2)
        {
            // Four points for line breaks.
            return 4;
        }
        else if (nonAlphaNumeric1 && !whitespace1 && whitespace2)
        {
            // Three points for end of sentences.
            return 3;
        }
        else if (whitespace1 || whitespace2)
        {
            // Two points for whitespace.
            return 2;
        }
        else if (nonAlphaNumeric1 || nonAlphaNumeric2)
        {
            // One point for non-alphanumeric.
            return 1;
        }
        return 0;
    }

    // Define some regex patterns for matching boundaries.
    private Regex BLANKLINEEND = new Regex("\\n\\r?\\n\\Z");
    private Regex BLANKLINESTART = new Regex("\\A\\r?\\n\\r?\\n");

    /**
     * Reduce the number of edits by eliminating operationally trivial
     * equalities.
     * @param diffs List of Diff objects.
     */
    public void DiffCleanupEfficiency(List<Diff> diffs)
    {
        bool changes = false;
        // Stack of indices where equalities are found.
        Stack<int> equalities = new Stack<int>();
        // Always equal to equalities[equalitiesLength-1][1]
        string lastequality = string.Empty;
        int pointer = 0;  // Index of current position.
        // Is there an insertion operation before the last equality.
        bool pre_ins = false;
        // Is there a deletion operation before the last equality.
        bool pre_del = false;
        // Is there an insertion operation after the last equality.
        bool post_ins = false;
        // Is there a deletion operation after the last equality.
        bool post_del = false;
        while (pointer < diffs.Count)
        {
            if (diffs[pointer].Operation == Operation.Equal)
            {  // Equality found.
                if (diffs[pointer].Text.Length < this.DiffEditCost
                    && (post_ins || post_del))
                {
                    // Candidate found.
                    equalities.Push(pointer);
                    pre_ins = post_ins;
                    pre_del = post_del;
                    lastequality = diffs[pointer].Text;
                }
                else
                {
                    // Not a candidate, and can never become one.
                    equalities.Clear();
                    lastequality = string.Empty;
                }
                post_ins = post_del = false;
            }
            else
            {  // An insertion or deletion.
                if (diffs[pointer].Operation == Operation.Delete)
                {
                    post_del = true;
                }
                else
                {
                    post_ins = true;
                }
                /*
                 * Five types to be split:
                 * <ins>A</ins><del>B</del>XY<ins>C</ins><del>D</del>
                 * <ins>A</ins>X<ins>C</ins><del>D</del>
                 * <ins>A</ins><del>B</del>X<ins>C</ins>
                 * <ins>A</del>X<ins>C</ins><del>D</del>
                 * <ins>A</ins><del>B</del>X<del>C</del>
                 */
                if ((lastequality.Length != 0)
                    && ((pre_ins && pre_del && post_ins && post_del)
                        || ((lastequality.Length < this.DiffEditCost / 2)
                            && ((pre_ins ? 1 : 0) + (pre_del ? 1 : 0) + (post_ins ? 1 : 0)
                                + (post_del ? 1 : 0)) == 3)))
                {
                    // Duplicate record.
                    diffs.Insert(equalities.Peek(),
                        new Diff(Operation.Delete, lastequality));
                    // Change second copy to insert.
                    diffs[equalities.Peek() + 1].Operation = Operation.Insert;
                    equalities.Pop();  // Throw away the equality we just deleted.
                    lastequality = string.Empty;
                    if (pre_ins && pre_del)
                    {
                        // No changes made which could affect previous entry, keep going.
                        post_ins = post_del = true;
                        equalities.Clear();
                    }
                    else
                    {
                        if (equalities.Count > 0)
                        {
                            equalities.Pop();
                        }

                        pointer = equalities.Count > 0 ? equalities.Peek() : -1;
                        post_ins = post_del = false;
                    }
                    changes = true;
                }
            }
            pointer++;
        }

        if (changes)
        {
            DiffCleanupMerge(diffs);
        }
    }

    /**
     * Reorder and merge like edit sections.  Merge equalities.
     * Any edit section can move as long as it doesn't cross an equality.
     * @param diffs List of Diff objects.
     */
    public void DiffCleanupMerge(List<Diff> diffs)
    {
        // Add a dummy entry at the end.
        diffs.Add(new Diff(Operation.Equal, string.Empty));
        int pointer = 0;
        int count_delete = 0;
        int count_insert = 0;
        string text_delete = string.Empty;
        string text_insert = string.Empty;
        int commonlength;
        while (pointer < diffs.Count)
        {
            switch (diffs[pointer].Operation)
            {
                case Operation.Insert:
                    count_insert++;
                    text_insert += diffs[pointer].Text;
                    pointer++;
                    break;
                case Operation.Delete:
                    count_delete++;
                    text_delete += diffs[pointer].Text;
                    pointer++;
                    break;
                case Operation.Equal:
                    // Upon reaching an equality, check for prior redundancies.
                    if (count_delete + count_insert > 1)
                    {
                        if (count_delete != 0 && count_insert != 0)
                        {
                            // Factor out any common prefixies.
                            commonlength = this.DiffCommonPrefix(text_insert, text_delete);
                            if (commonlength != 0)
                            {
                                if ((pointer - count_delete - count_insert) > 0 &&
                                    diffs[pointer - count_delete - count_insert - 1].Operation
                                    == Operation.Equal)
                                {
                                    diffs[pointer - count_delete - count_insert - 1].Text
                                        += text_insert.Substring(0, commonlength);
                                }
                                else
                                {
                                    diffs.Insert(0, new Diff(Operation.Equal,
                                        text_insert.Substring(0, commonlength)));
                                    pointer++;
                                }
                                text_insert = text_insert.Substring(commonlength);
                                text_delete = text_delete.Substring(commonlength);
                            }
                            // Factor out any common suffixies.
                            commonlength = this.DiffCommonSuffix(text_insert, text_delete);
                            if (commonlength != 0)
                            {
                                diffs[pointer].Text = text_insert.Substring(text_insert.Length
                                                                            - commonlength) + diffs[pointer].Text;
                                text_insert = text_insert.Substring(0, text_insert.Length
                                                                       - commonlength);
                                text_delete = text_delete.Substring(0, text_delete.Length
                                                                       - commonlength);
                            }
                        }
                        // Delete the offending records and add the merged ones.
                        if (count_delete == 0)
                        {
                            diffs.Splice(pointer - count_insert,
                                count_delete + count_insert,
                                new Diff(Operation.Insert, text_insert));
                        }
                        else if (count_insert == 0)
                        {
                            diffs.Splice(pointer - count_delete,
                                count_delete + count_insert,
                                new Diff(Operation.Delete, text_delete));
                        }
                        else
                        {
                            diffs.Splice(pointer - count_delete - count_insert,
                                count_delete + count_insert,
                                new Diff(Operation.Delete, text_delete),
                                new Diff(Operation.Insert, text_insert));
                        }
                        pointer = pointer - count_delete - count_insert +
                                  (count_delete != 0 ? 1 : 0) + (count_insert != 0 ? 1 : 0) + 1;
                    }
                    else if (pointer != 0
                             && diffs[pointer - 1].Operation == Operation.Equal)
                    {
                        // Merge this equality with the previous one.
                        diffs[pointer - 1].Text += diffs[pointer].Text;
                        diffs.RemoveAt(pointer);
                    }
                    else
                    {
                        pointer++;
                    }
                    count_insert = 0;
                    count_delete = 0;
                    text_delete = string.Empty;
                    text_insert = string.Empty;
                    break;
            }
        }
        if (diffs[diffs.Count - 1].Text.Length == 0)
        {
            diffs.RemoveAt(diffs.Count - 1);  // Remove the dummy entry at the end.
        }

        // Second pass: look for single edits surrounded on both sides by
        // equalities which can be shifted sideways to eliminate an equality.
        // e.g: A<ins>BA</ins>C -> <ins>AB</ins>AC
        bool changes = false;
        pointer = 1;
        // Intentionally ignore the first and last element (don't need checking).
        while (pointer < (diffs.Count - 1))
        {
            if (diffs[pointer - 1].Operation == Operation.Equal &&
                diffs[pointer + 1].Operation == Operation.Equal)
            {
                // This is a single edit surrounded by equalities.
                if (diffs[pointer].Text.EndsWith(diffs[pointer - 1].Text,
                        StringComparison.Ordinal))
                {
                    // Shift the edit over the previous equality.
                    diffs[pointer].Text = diffs[pointer - 1].Text +
                                          diffs[pointer].Text.Substring(0, diffs[pointer].Text.Length -
                                                                           diffs[pointer - 1].Text.Length);
                    diffs[pointer + 1].Text = diffs[pointer - 1].Text
                                              + diffs[pointer + 1].Text;
                    diffs.Splice(pointer - 1, 1);
                    changes = true;
                }
                else if (diffs[pointer].Text.StartsWith(diffs[pointer + 1].Text,
                             StringComparison.Ordinal))
                {
                    // Shift the edit over the next equality.
                    diffs[pointer - 1].Text += diffs[pointer + 1].Text;
                    diffs[pointer].Text =
                        diffs[pointer].Text.Substring(diffs[pointer + 1].Text.Length)
                        + diffs[pointer + 1].Text;
                    diffs.Splice(pointer + 1, 1);
                    changes = true;
                }
            }
            pointer++;
        }
        // If shifts were made, the diff needs reordering and another shift sweep.
        if (changes)
        {
            this.DiffCleanupMerge(diffs);
        }
    }

    /**
     * loc is a location in text1, comAdde and return the equivalent location in
     * text2.
     * e.g. "The cat" vs "The big cat", 1->1, 5->8
     * @param diffs List of Diff objects.
     * @param loc Location within text1.
     * @return Location within text2.
     */
    public int DiffXIndex(List<Diff> diffs, int loc)
    {
        int chars1 = 0;
        int chars2 = 0;
        int last_chars1 = 0;
        int last_chars2 = 0;
        Diff lastDiff = null;
        foreach (Diff aDiff in diffs)
        {
            if (aDiff.Operation != Operation.Insert)
            {
                // Equality or deletion.
                chars1 += aDiff.Text.Length;
            }
            if (aDiff.Operation != Operation.Delete)
            {
                // Equality or insertion.
                chars2 += aDiff.Text.Length;
            }
            if (chars1 > loc)
            {
                // Overshot the location.
                lastDiff = aDiff;
                break;
            }
            last_chars1 = chars1;
            last_chars2 = chars2;
        }
        if (lastDiff != null && lastDiff.Operation == Operation.Delete)
        {
            // The location was deleted.
            return last_chars2;
        }
        // Add the remaining character length.
        return last_chars2 + (loc - last_chars1);
    }

    /**
     * Convert a Diff list into a pretty HTML report.
     * @param diffs List of Diff objects.
     * @return HTML representation.
     */
    public string DiffPrettyHtml(List<Diff> diffs)
    {
        StringBuilder html = new StringBuilder();
        foreach (Diff aDiff in diffs)
        {
            string text = aDiff.Text.Replace("&", "&amp;").Replace("<", "&lt;")
                .Replace(">", "&gt;").Replace("\n", "&para;<br>");
            switch (aDiff.Operation)
            {
                case Operation.Insert:
                    html.Append("<ins style=\"background:#e6ffe6;\">").Append(text)
                        .Append("</ins>");
                    break;
                case Operation.Delete:
                    html.Append("<del style=\"background:#ffe6e6;\">").Append(text)
                        .Append("</del>");
                    break;
                case Operation.Equal:
                    html.Append("<span>").Append(text).Append("</span>");
                    break;
            }
        }
        return html.ToString();
    }

    /**
     * Compute and return the source text (all equalities and deletions).
     * @param diffs List of Diff objects.
     * @return Source text.
     */
    public string DiffText1(List<Diff> diffs)
    {
        StringBuilder text = new StringBuilder();
        foreach (Diff aDiff in diffs)
        {
            if (aDiff.Operation != Operation.Insert)
            {
                text.Append(aDiff.Text);
            }
        }
        return text.ToString();
    }

    /**
     * Compute and return the destination text (all equalities and insertions).
     * @param diffs List of Diff objects.
     * @return Destination text.
     */
    public string DiffText2(List<Diff> diffs)
    {
        StringBuilder text = new StringBuilder();
        foreach (Diff aDiff in diffs)
        {
            if (aDiff.Operation != Operation.Delete)
            {
                text.Append(aDiff.Text);
            }
        }
        return text.ToString();
    }

    /**
     * Compute the Levenshtein distance; the number of inserted, deleted or
     * substituted characters.
     * @param diffs List of Diff objects.
     * @return Number of changes.
     */
    public int DiffLevenshtein(List<Diff> diffs)
    {
        int levenshtein = 0;
        int insertions = 0;
        int deletions = 0;
        foreach (Diff aDiff in diffs)
        {
            switch (aDiff.Operation)
            {
                case Operation.Insert:
                    insertions += aDiff.Text.Length;
                    break;
                case Operation.Delete:
                    deletions += aDiff.Text.Length;
                    break;
                case Operation.Equal:
                    // A deletion and an insertion is one substitution.
                    levenshtein += Math.Max(insertions, deletions);
                    insertions = 0;
                    deletions = 0;
                    break;
            }
        }
        levenshtein += Math.Max(insertions, deletions);
        return levenshtein;
    }

    /**
     * Crush the diff into an encoded string which describes the operations
     * required to transform text1 into text2.
     * E.g. =3\t-2\t+ing  -> Keep 3 chars, delete 2 chars, insert 'ing'.
     * Operations are tab-separated.  Inserted text is escaped using %xx
     * notation.
     * @param diffs Array of Diff objects.
     * @return Delta text.
     */
    public string DiffToDelta(List<Diff> diffs)
    {
        StringBuilder text = new StringBuilder();
        foreach (Diff aDiff in diffs)
        {
            switch (aDiff.Operation)
            {
                case Operation.Insert:
                    text.Append("+").Append(HttpUtility.UrlEncode(aDiff.Text,
                        new UTF8Encoding()).Replace('+', ' ')).Append("\t");
                    break;
                case Operation.Delete:
                    text.Append("-").Append(aDiff.Text.Length).Append("\t");
                    break;
                case Operation.Equal:
                    text.Append("=").Append(aDiff.Text.Length).Append("\t");
                    break;
            }
        }
        string delta = text.ToString();
        if (delta.Length != 0)
        {
            // Strip off trailing tab character.
            delta = delta.Substring(0, delta.Length - 1);
            delta = UnescapeForEncodeUriCompatability(delta);
        }
        return delta;
    }

    /**
     * Given the original text1, and an encoded string which describes the
     * operations required to transform text1 into text2, comAdde the full diff.
     * @param text1 Source string for the diff.
     * @param delta Delta text.
     * @return Array of Diff objects or null if invalid.
     * @throws ArgumentException If invalid input.
     */
    public List<Diff> DiffFromDelta(string text1, string delta)
    {
        List<Diff> diffs = new List<Diff>();
        int pointer = 0;  // Cursor in text1
        string[] tokens = delta.Split(new string[] { "\t" },
            StringSplitOptions.None);
        foreach (string token in tokens)
        {
            if (token.Length == 0)
            {
                // Blank tokens are ok (from a trailing \t).
                continue;
            }
            // Each token begins with a one character parameter which specifies the
            // operation of this token (delete, insert, equality).
            string param = token.Substring(1);
            switch (token[0])
            {
                case '+':
                    // decode would change all "+" to " "
                    param = param.Replace("+", "%2b");

                    param = HttpUtility.UrlDecode(param, new UTF8Encoding(false, true));
                    //} catch (UnsupportedEncodingException e) {
                    //  // Not likely on modern system.
                    //  throw new Error("This system does not support UTF-8.", e);
                    //} catch (IllegalArgumentException e) {
                    //  // Malformed URI sequence.
                    //  throw new IllegalArgumentException(
                    //      "Illegal escape in diff_fromDelta: " + param, e);
                    //}
                    diffs.Add(new Diff(Operation.Insert, param));
                    break;
                case '-':
                // Fall through.
                case '=':
                    int n;
                    try
                    {
                        n = Convert.ToInt32(param);
                    }
                    catch (FormatException e)
                    {
                        throw new ArgumentException(
                            "Invalid number in diff_fromDelta: " + param, e);
                    }
                    if (n < 0)
                    {
                        throw new ArgumentException(
                            "Negative number in diff_fromDelta: " + param);
                    }
                    string text;
                    try
                    {
                        text = text1.Substring(pointer, n);
                        pointer += n;
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        throw new ArgumentException("Delta length (" + pointer
                                                                     + ") larger than source text length (" + text1.Length
                                                                     + ").", e);
                    }
                    if (token[0] == '=')
                    {
                        diffs.Add(new Diff(Operation.Equal, text));
                    }
                    else
                    {
                        diffs.Add(new Diff(Operation.Delete, text));
                    }
                    break;
                default:
                    // Anything else is an error.
                    throw new ArgumentException(
                        "Invalid diff operation in diff_fromDelta: " + token[0]);
            }
        }
        if (pointer != text1.Length)
        {
            throw new ArgumentException("Delta length (" + pointer
                                                         + ") smaller than source text length (" + text1.Length + ").");
        }
        return diffs;
    }


    //  MATCH FUNCTIONS


    /**
     * Locate the best instance of 'pattern' in 'text' near 'loc'.
     * Returns -1 if no match found.
     * @param text The text to search.
     * @param pattern The pattern to search for.
     * @param loc The location to search around.
     * @return Best match index or -1.
     */
    public int MatchMain(string text, string pattern, int loc)
    {
        // Check for null inputs not needed since null can't be passed in C#.

        loc = Math.Max(0, Math.Min(loc, text.Length));
        if (text == pattern)
        {
            // Shortcut (potentially not guaranteed by the algorithm)
            return 0;
        }
        else if (text.Length == 0)
        {
            // Nothing to match.
            return -1;
        }
        else if (loc + pattern.Length <= text.Length
                 && text.Substring(loc, pattern.Length) == pattern)
        {
            // Perfect match at the perfect spot!  (Includes case of null pattern)
            return loc;
        }
        else
        {
            // Do a fuzzy compare.
            return MatchBitap(text, pattern, loc);
        }
    }

    /**
     * Locate the best instance of 'pattern' in 'text' near 'loc' using the
     * Bitap algorithm.  Returns -1 if no match found.
     * @param text The text to search.
     * @param pattern The pattern to search for.
     * @param loc The location to search around.
     * @return Best match index or -1.
     */
    protected int MatchBitap(string text, string pattern, int loc)
    {
        // assert (Match_MaxBits == 0 || pattern.Length <= Match_MaxBits)
        //    : "Pattern too long for this application.";

        // Initialise the alphabet.
        Dictionary<char, int> s = MatchAlphabet(pattern);

        // Highest score beyond which we give up.
        double score_threshold = MatchThreshold;
        // Is there a nearby exact match? (speedup)
        int best_loc = text.IndexOf(pattern, loc, StringComparison.Ordinal);
        if (best_loc != -1)
        {
            score_threshold = Math.Min(MatchBitapScore(0, best_loc, loc,
                pattern), score_threshold);
            // What about in the other direction? (speedup)
            best_loc = text.LastIndexOf(pattern,
                Math.Min(loc + pattern.Length, text.Length),
                StringComparison.Ordinal);
            if (best_loc != -1)
            {
                score_threshold = Math.Min(MatchBitapScore(0, best_loc, loc,
                    pattern), score_threshold);
            }
        }

        // Initialise the bit arrays.
        int matchmask = 1 << (pattern.Length - 1);
        best_loc = -1;

        int bin_min, bin_mid;
        int bin_max = pattern.Length + text.Length;
        // Empty initialization added to appease C# compiler.
        int[] last_rd = new int[0];
        for (int d = 0; d < pattern.Length; d++)
        {
            // Scan for the best match; each iteration allows for one more error.
            // Run a binary search to determine how far from 'loc' we can stray at
            // this error level.
            bin_min = 0;
            bin_mid = bin_max;
            while (bin_min < bin_mid)
            {
                if (MatchBitapScore(d, loc + bin_mid, loc, pattern)
                    <= score_threshold)
                {
                    bin_min = bin_mid;
                }
                else
                {
                    bin_max = bin_mid;
                }
                bin_mid = (bin_max - bin_min) / 2 + bin_min;
            }
            // Use the result from this iteration as the maximum for the next.
            bin_max = bin_mid;
            int start = Math.Max(1, loc - bin_mid + 1);
            int finish = Math.Min(loc + bin_mid, text.Length) + pattern.Length;

            int[] rd = new int[finish + 2];
            rd[finish + 1] = (1 << d) - 1;
            for (int j = finish; j >= start; j--)
            {
                int charMatch;
                if (text.Length <= j - 1 || !s.ContainsKey(text[j - 1]))
                {
                    // Out of range.
                    charMatch = 0;
                }
                else
                {
                    charMatch = s[text[j - 1]];
                }
                if (d == 0)
                {
                    // First pass: exact match.
                    rd[j] = ((rd[j + 1] << 1) | 1) & charMatch;
                }
                else
                {
                    // Subsequent passes: fuzzy match.
                    rd[j] = ((rd[j + 1] << 1) | 1) & charMatch
                            | (((last_rd[j + 1] | last_rd[j]) << 1) | 1) | last_rd[j + 1];
                }
                if ((rd[j] & matchmask) != 0)
                {
                    double score = MatchBitapScore(d, j - 1, loc, pattern);
                    // This match will almost certainly be better than any existing
                    // match.  But check anyway.
                    if (score <= score_threshold)
                    {
                        // Told you so.
                        score_threshold = score;
                        best_loc = j - 1;
                        if (best_loc > loc)
                        {
                            // When passing loc, don't exceed our current distance from loc.
                            start = Math.Max(1, 2 * loc - best_loc);
                        }
                        else
                        {
                            // Already passed loc, downhill from here on in.
                            break;
                        }
                    }
                }
            }
            if (MatchBitapScore(d + 1, loc, loc, pattern) > score_threshold)
            {
                // No hope for a (better) match at greater error levels.
                break;
            }
            last_rd = rd;
        }
        return best_loc;
    }

    /**
     * Compute and return the score for a match with e errors and x location.
     * @param e Number of errors in match.
     * @param x Location of match.
     * @param loc Expected location of match.
     * @param pattern Pattern being sought.
     * @return Overall score for match (0.0 = good, 1.0 = bad).
     */
    private double MatchBitapScore(int e, int x, int loc, string pattern)
    {
        float accuracy = (float)e / pattern.Length;
        int proximity = Math.Abs(loc - x);
        if (MatchDistance == 0)
        {
            // Dodge divide by zero error.
            return proximity == 0 ? accuracy : 1.0;
        }
        return accuracy + (proximity / (float)MatchDistance);
    }

    /**
     * Initialise the alphabet for the Bitap algorithm.
     * @param pattern The text to encode.
     * @return Hash of character locations.
     */
    protected Dictionary<char, int> MatchAlphabet(string pattern)
    {
        Dictionary<char, int> s = new Dictionary<char, int>();
        char[] char_pattern = pattern.ToCharArray();
        foreach (char c in char_pattern)
        {
            if (!s.ContainsKey(c))
            {
                s.Add(c, 0);
            }
        }
        int i = 0;
        foreach (char c in char_pattern)
        {
            int value = s[c] | (1 << (pattern.Length - i - 1));
            s[c] = value;
            i++;
        }
        return s;
    }


    //  PATCH FUNCTIONS


    /**
     * Increase the context until it is unique,
     * but don't let the pattern expand beyond Match_MaxBits.
     * @param patch The patch to grow.
     * @param text Source text.
     */
    protected void PatchAddContext(Patch patch, string text)
    {
        if (text.Length == 0)
        {
            return;
        }
        string pattern = text.Substring(patch.Start2, patch.Length1);
        int padding = 0;

        // Look for the first and last matches of pattern in text.  If two
        // different matches are found, increase the pattern length.
        while (text.IndexOf(pattern, StringComparison.Ordinal)
               != text.LastIndexOf(pattern, StringComparison.Ordinal)
               && pattern.Length < MatchMaxBits - PatchMargin - PatchMargin)
        {
            padding += PatchMargin;
            pattern = text.JavaSubstring(Math.Max(0, patch.Start2 - padding),
                Math.Min(text.Length, patch.Start2 + patch.Length1 + padding));
        }
        // Add one chunk for good luck.
        padding += PatchMargin;

        // Add the prefix.
        string prefix = text.JavaSubstring(Math.Max(0, patch.Start2 - padding),
            patch.Start2);
        if (prefix.Length != 0)
        {
            patch.Diffs.Insert(0, new Diff(Operation.Equal, prefix));
        }
        // Add the suffix.
        string suffix = text.JavaSubstring(patch.Start2 + patch.Length1,
            Math.Min(text.Length, patch.Start2 + patch.Length1 + padding));
        if (suffix.Length != 0)
        {
            patch.Diffs.Add(new Diff(Operation.Equal, suffix));
        }

        // Roll back the start points.
        patch.Start1 -= prefix.Length;
        patch.Start2 -= prefix.Length;
        // Extend the lengths.
        patch.Length1 += prefix.Length + suffix.Length;
        patch.Length2 += prefix.Length + suffix.Length;
    }

    /**
     * Compute a list of patches to turn text1 into text2.
     * A set of diffs will be computed.
     * @param text1 Old text.
     * @param text2 New text.
     * @return List of Patch objects.
     */
    public List<Patch> PatchMake(string text1, string text2)
    {
        // Check for null inputs not needed since null can't be passed in C#.
        // No diffs provided, comAdde our own.
        List<Diff> diffs = DiffMain(text1, text2, true);
        if (diffs.Count > 2)
        {
            DiffCleanupSemantic(diffs);
            DiffCleanupEfficiency(diffs);
        }
        return PatchMake(text1, diffs);
    }

    /**
     * Compute a list of patches to turn text1 into text2.
     * text1 will be derived from the provided diffs.
     * @param diffs Array of Diff objects for text1 to text2.
     * @return List of Patch objects.
     */
    public List<Patch> PatchMake(List<Diff> diffs)
    {
        // Check for null inputs not needed since null can't be passed in C#.
        // No origin string provided, comAdde our own.
        string text1 = DiffText1(diffs);
        return PatchMake(text1, diffs);
    }

    /**
     * Compute a list of patches to turn text1 into text2.
     * text2 is ignored, diffs are the delta between text1 and text2.
     * @param text1 Old text
     * @param text2 Ignored.
     * @param diffs Array of Diff objects for text1 to text2.
     * @return List of Patch objects.
     * @deprecated Prefer patch_make(string text1, List<Diff> diffs).
     */
    public List<Patch> PatchMake(string text1, string text2,
        List<Diff> diffs)
    {
        return PatchMake(text1, diffs);
    }

    /**
     * Compute a list of patches to turn text1 into text2.
     * text2 is not provided, diffs are the delta between text1 and text2.
     * @param text1 Old text.
     * @param diffs Array of Diff objects for text1 to text2.
     * @return List of Patch objects.
     */
    public List<Patch> PatchMake(string text1, List<Diff> diffs)
    {
        // Check for null inputs not needed since null can't be passed in C#.
        List<Patch> patches = new List<Patch>();
        if (diffs.Count == 0)
        {
            return patches;  // Get rid of the null case.
        }
        Patch patch = new Patch();
        int char_count1 = 0;  // Number of characters into the text1 string.
        int char_count2 = 0;  // Number of characters into the text2 string.
        // Start with text1 (prepatch_text) and apply the diffs until we arrive at
        // text2 (postpatch_text). We recreate the patches one by one to determine
        // context info.
        string prepatch_text = text1;
        string postpatch_text = text1;
        foreach (Diff aDiff in diffs)
        {
            if (patch.Diffs.Count == 0 && aDiff.Operation != Operation.Equal)
            {
                // A new patch starts here.
                patch.Start1 = char_count1;
                patch.Start2 = char_count2;
            }

            switch (aDiff.Operation)
            {
                case Operation.Insert:
                    patch.Diffs.Add(aDiff);
                    patch.Length2 += aDiff.Text.Length;
                    postpatch_text = postpatch_text.Insert(char_count2, aDiff.Text);
                    break;
                case Operation.Delete:
                    patch.Length1 += aDiff.Text.Length;
                    patch.Diffs.Add(aDiff);
                    postpatch_text = postpatch_text.Remove(char_count2,
                        aDiff.Text.Length);
                    break;
                case Operation.Equal:
                    if (aDiff.Text.Length <= 2 * PatchMargin
                        && patch.Diffs.Count() != 0 && aDiff != diffs.Last())
                    {
                        // Small equality inside a patch.
                        patch.Diffs.Add(aDiff);
                        patch.Length1 += aDiff.Text.Length;
                        patch.Length2 += aDiff.Text.Length;
                    }

                    if (aDiff.Text.Length >= 2 * PatchMargin)
                    {
                        // Time for a new patch.
                        if (patch.Diffs.Count != 0)
                        {
                            PatchAddContext(patch, prepatch_text);
                            patches.Add(patch);
                            patch = new Patch();
                            // Unlike Unidiff, our patch lists have a rolling context.
                            // http://code.google.com/p/google-diff-match-patch/wiki/Unidiff
                            // Update prepatch text & pos to reflect the application of the
                            // just completed patch.
                            prepatch_text = postpatch_text;
                            char_count1 = char_count2;
                        }
                    }
                    break;
            }

            // Update the current character count.
            if (aDiff.Operation != Operation.Insert)
            {
                char_count1 += aDiff.Text.Length;
            }
            if (aDiff.Operation != Operation.Delete)
            {
                char_count2 += aDiff.Text.Length;
            }
        }
        // Pick up the leftover patch if not empty.
        if (patch.Diffs.Count != 0)
        {
            PatchAddContext(patch, prepatch_text);
            patches.Add(patch);
        }

        return patches;
    }

    /**
     * Given an array of patches, return another array that is identical.
     * @param patches Array of Patch objects.
     * @return Array of Patch objects.
     */
    public List<Patch> PatchDeepCopy(List<Patch> patches)
    {
        List<Patch> patchesCopy = new List<Patch>();
        foreach (Patch aPatch in patches)
        {
            Patch patchCopy = new Patch();
            foreach (Diff aDiff in aPatch.Diffs)
            {
                Diff diffCopy = new Diff(aDiff.Operation, aDiff.Text);
                patchCopy.Diffs.Add(diffCopy);
            }
            patchCopy.Start1 = aPatch.Start1;
            patchCopy.Start2 = aPatch.Start2;
            patchCopy.Length1 = aPatch.Length1;
            patchCopy.Length2 = aPatch.Length2;
            patchesCopy.Add(patchCopy);
        }
        return patchesCopy;
    }

    /**
     * Merge a set of patches onto the text.  Return a patched text, as well
     * as an array of true/false values indicating which patches were applied.
     * @param patches Array of Patch objects
     * @param text Old text.
     * @return Two element Object array, containing the new text and an array of
     *      bool values.
     */
    public Object[] PatchApply(List<Patch> patches, string text)
    {
        if (patches.Count == 0)
        {
            return new Object[] { text, new bool[0] };
        }

        // Deep copy the patches so that no changes are made to originals.
        patches = PatchDeepCopy(patches);

        string nullPadding = this.PatchAddPadding(patches);
        text = nullPadding + text + nullPadding;
        PatchSplitMax(patches);

        int x = 0;
        // delta keeps track of the offset between the expected and actual
        // location of the previous patch.  If there are patches expected at
        // positions 10 and 20, but the first patch was found at 12, delta is 2
        // and the second patch has an effective expected position of 22.
        int delta = 0;
        bool[] results = new bool[patches.Count];
        foreach (Patch aPatch in patches)
        {
            int expected_loc = aPatch.Start2 + delta;
            string text1 = DiffText1(aPatch.Diffs);
            int start_loc;
            int end_loc = -1;
            if (text1.Length > this.MatchMaxBits)
            {
                // patch_splitMax will only provide an oversized pattern
                // in the case of a monster delete.
                start_loc = MatchMain(text,
                    text1.Substring(0, this.MatchMaxBits), expected_loc);
                if (start_loc != -1)
                {
                    end_loc = MatchMain(text,
                        text1.Substring(text1.Length - this.MatchMaxBits),
                        expected_loc + text1.Length - this.MatchMaxBits);
                    if (end_loc == -1 || start_loc >= end_loc)
                    {
                        // Can't find valid trailing context.  Drop this patch.
                        start_loc = -1;
                    }
                }
            }
            else
            {
                start_loc = this.MatchMain(text, text1, expected_loc);
            }
            if (start_loc == -1)
            {
                // No match found.  :(
                results[x] = false;
                // Subtract the delta for this failed patch from subsequent patches.
                delta -= aPatch.Length2 - aPatch.Length1;
            }
            else
            {
                // Found a match.  :)
                results[x] = true;
                delta = start_loc - expected_loc;
                string text2;
                if (end_loc == -1)
                {
                    text2 = text.JavaSubstring(start_loc,
                        Math.Min(start_loc + text1.Length, text.Length));
                }
                else
                {
                    text2 = text.JavaSubstring(start_loc,
                        Math.Min(end_loc + this.MatchMaxBits, text.Length));
                }
                if (text1 == text2)
                {
                    // Perfect match, just shove the Replacement text in.
                    text = text.Substring(0, start_loc) + DiffText2(aPatch.Diffs)
                                                        + text.Substring(start_loc + text1.Length);
                }
                else
                {
                    // Imperfect match.  Run a diff to get a framework of equivalent
                    // indices.
                    List<Diff> diffs = DiffMain(text1, text2, false);
                    if (text1.Length > this.MatchMaxBits
                        && this.DiffLevenshtein(diffs) / (float)text1.Length
                        > this.PatchDeleteThreshold)
                    {
                        // The end points match, but the content is unacceptably bad.
                        results[x] = false;
                    }
                    else
                    {
                        DiffCleanupSemanticLossless(diffs);
                        int index1 = 0;
                        foreach (Diff aDiff in aPatch.Diffs)
                        {
                            if (aDiff.Operation != Operation.Equal)
                            {
                                int index2 = DiffXIndex(diffs, index1);
                                if (aDiff.Operation == Operation.Insert)
                                {
                                    // Insertion
                                    text = text.Insert(start_loc + index2, aDiff.Text);
                                }
                                else if (aDiff.Operation == Operation.Delete)
                                {
                                    // Deletion
                                    text = text.Remove(start_loc + index2, DiffXIndex(diffs,
                                        index1 + aDiff.Text.Length) - index2);
                                }
                            }
                            if (aDiff.Operation != Operation.Delete)
                            {
                                index1 += aDiff.Text.Length;
                            }
                        }
                    }
                }
            }
            x++;
        }
        // Strip the padding off.
        text = text.Substring(nullPadding.Length, text.Length
                                                  - 2 * nullPadding.Length);
        return new Object[] { text, results };
    }

    /**
     * Add some padding on text start and end so that edges can match something.
     * Intended to be called only from within patch_apply.
     * @param patches Array of Patch objects.
     * @return The padding string added to each side.
     */
    public string PatchAddPadding(List<Patch> patches)
    {
        short paddingLength = this.PatchMargin;
        string nullPadding = string.Empty;
        for (short x = 1; x <= paddingLength; x++)
        {
            nullPadding += (char)x;
        }

        // Bump all the patches forward.
        foreach (Patch aPatch in patches)
        {
            aPatch.Start1 += paddingLength;
            aPatch.Start2 += paddingLength;
        }

        // Add some padding on start of first diff.
        Patch patch = patches.First();
        List<Diff> diffs = patch.Diffs;
        if (diffs.Count == 0 || diffs.First().Operation != Operation.Equal)
        {
            // Add nullPadding equality.
            diffs.Insert(0, new Diff(Operation.Equal, nullPadding));
            patch.Start1 -= paddingLength;  // Should be 0.
            patch.Start2 -= paddingLength;  // Should be 0.
            patch.Length1 += paddingLength;
            patch.Length2 += paddingLength;
        }
        else if (paddingLength > diffs.First().Text.Length)
        {
            // Grow first equality.
            Diff firstDiff = diffs.First();
            int extraLength = paddingLength - firstDiff.Text.Length;
            firstDiff.Text = nullPadding.Substring(firstDiff.Text.Length)
                             + firstDiff.Text;
            patch.Start1 -= extraLength;
            patch.Start2 -= extraLength;
            patch.Length1 += extraLength;
            patch.Length2 += extraLength;
        }

        // Add some padding on end of last diff.
        patch = patches.Last();
        diffs = patch.Diffs;
        if (diffs.Count == 0 || diffs.Last().Operation != Operation.Equal)
        {
            // Add nullPadding equality.
            diffs.Add(new Diff(Operation.Equal, nullPadding));
            patch.Length1 += paddingLength;
            patch.Length2 += paddingLength;
        }
        else if (paddingLength > diffs.Last().Text.Length)
        {
            // Grow last equality.
            Diff lastDiff = diffs.Last();
            int extraLength = paddingLength - lastDiff.Text.Length;
            lastDiff.Text += nullPadding.Substring(0, extraLength);
            patch.Length1 += extraLength;
            patch.Length2 += extraLength;
        }

        return nullPadding;
    }

    /**
     * Look through the patches and break up any which are longer than the
     * maximum limit of the match algorithm.
     * Intended to be called only from within patch_apply.
     * @param patches List of Patch objects.
     */
    public void PatchSplitMax(List<Patch> patches)
    {
        short patch_size = this.MatchMaxBits;
        for (int x = 0; x < patches.Count; x++)
        {
            if (patches[x].Length1 <= patch_size)
            {
                continue;
            }
            Patch bigpatch = patches[x];
            // Remove the big old patch.
            patches.Splice(x--, 1);
            int start1 = bigpatch.Start1;
            int start2 = bigpatch.Start2;
            string precontext = string.Empty;
            while (bigpatch.Diffs.Count != 0)
            {
                // Create one of several smaller patches.
                Patch patch = new Patch();
                bool empty = true;
                patch.Start1 = start1 - precontext.Length;
                patch.Start2 = start2 - precontext.Length;
                if (precontext.Length != 0)
                {
                    patch.Length1 = patch.Length2 = precontext.Length;
                    patch.Diffs.Add(new Diff(Operation.Equal, precontext));
                }
                while (bigpatch.Diffs.Count != 0
                       && patch.Length1 < patch_size - this.PatchMargin)
                {
                    Operation diff_type = bigpatch.Diffs[0].Operation;
                    string diff_text = bigpatch.Diffs[0].Text;
                    if (diff_type == Operation.Insert)
                    {
                        // Insertions are harmless.
                        patch.Length2 += diff_text.Length;
                        start2 += diff_text.Length;
                        patch.Diffs.Add(bigpatch.Diffs.First());
                        bigpatch.Diffs.RemoveAt(0);
                        empty = false;
                    }
                    else if (diff_type == Operation.Delete && patch.Diffs.Count == 1
                                                           && patch.Diffs.First().Operation == Operation.Equal
                                                           && diff_text.Length > 2 * patch_size)
                    {
                        // This is a large deletion.  Let it pass in one chunk.
                        patch.Length1 += diff_text.Length;
                        start1 += diff_text.Length;
                        empty = false;
                        patch.Diffs.Add(new Diff(diff_type, diff_text));
                        bigpatch.Diffs.RemoveAt(0);
                    }
                    else
                    {
                        // Deletion or equality.  Only take as much as we can stomach.
                        diff_text = diff_text.Substring(0, Math.Min(diff_text.Length,
                            patch_size - patch.Length1 - PatchMargin));
                        patch.Length1 += diff_text.Length;
                        start1 += diff_text.Length;
                        if (diff_type == Operation.Equal)
                        {
                            patch.Length2 += diff_text.Length;
                            start2 += diff_text.Length;
                        }
                        else
                        {
                            empty = false;
                        }
                        patch.Diffs.Add(new Diff(diff_type, diff_text));
                        if (diff_text == bigpatch.Diffs[0].Text)
                        {
                            bigpatch.Diffs.RemoveAt(0);
                        }
                        else
                        {
                            bigpatch.Diffs[0].Text =
                                bigpatch.Diffs[0].Text.Substring(diff_text.Length);
                        }
                    }
                }
                // Compute the head context for the next patch.
                precontext = this.DiffText2(patch.Diffs);
                precontext = precontext.Substring(Math.Max(0,
                    precontext.Length - this.PatchMargin));

                string postcontext = null;
                // Append the end context for this patch.
                if (DiffText1(bigpatch.Diffs).Length > PatchMargin)
                {
                    postcontext = DiffText1(bigpatch.Diffs)
                        .Substring(0, PatchMargin);
                }
                else
                {
                    postcontext = DiffText1(bigpatch.Diffs);
                }

                if (postcontext.Length != 0)
                {
                    patch.Length1 += postcontext.Length;
                    patch.Length2 += postcontext.Length;
                    if (patch.Diffs.Count != 0
                        && patch.Diffs[patch.Diffs.Count - 1].Operation
                        == Operation.Equal)
                    {
                        patch.Diffs[patch.Diffs.Count - 1].Text += postcontext;
                    }
                    else
                    {
                        patch.Diffs.Add(new Diff(Operation.Equal, postcontext));
                    }
                }
                if (!empty)
                {
                    patches.Splice(++x, 0, patch);
                }
            }
        }
    }

    /**
     * Take a list of patches and return a textual representation.
     * @param patches List of Patch objects.
     * @return Text representation of patches.
     */
    public string PatchToText(List<Patch> patches)
    {
        StringBuilder text = new StringBuilder();
        foreach (Patch aPatch in patches)
        {
            text.Append(aPatch);
        }
        return text.ToString();
    }

    /**
     * Parse a textual representation of patches and return a List of Patch
     * objects.
     * @param textline Text representation of patches.
     * @return List of Patch objects.
     * @throws ArgumentException If invalid input.
     */
    public List<Patch> PatchFromText(string textline)
    {
        List<Patch> patches = new List<Patch>();
        if (textline.Length == 0)
        {
            return patches;
        }
        string[] text = textline.Split('\n');
        int textPointer = 0;
        Patch patch;
        Regex patchHeader
            = new Regex("^@@ -(\\d+),?(\\d*) \\+(\\d+),?(\\d*) @@$");
        Match m;
        char sign;
        string line;
        while (textPointer < text.Length)
        {
            m = patchHeader.Match(text[textPointer]);
            if (!m.Success)
            {
                throw new ArgumentException("Invalid patch string: "
                                            + text[textPointer]);
            }
            patch = new Patch();
            patches.Add(patch);
            patch.Start1 = Convert.ToInt32(m.Groups[1].Value);
            if (m.Groups[2].Length == 0)
            {
                patch.Start1--;
                patch.Length1 = 1;
            }
            else if (m.Groups[2].Value == "0")
            {
                patch.Length1 = 0;
            }
            else
            {
                patch.Start1--;
                patch.Length1 = Convert.ToInt32(m.Groups[2].Value);
            }

            patch.Start2 = Convert.ToInt32(m.Groups[3].Value);
            if (m.Groups[4].Length == 0)
            {
                patch.Start2--;
                patch.Length2 = 1;
            }
            else if (m.Groups[4].Value == "0")
            {
                patch.Length2 = 0;
            }
            else
            {
                patch.Start2--;
                patch.Length2 = Convert.ToInt32(m.Groups[4].Value);
            }
            textPointer++;

            while (textPointer < text.Length)
            {
                try
                {
                    sign = text[textPointer][0];
                }
                catch (IndexOutOfRangeException)
                {
                    // Blank line?  Whatever.
                    textPointer++;
                    continue;
                }
                line = text[textPointer].Substring(1);
                line = line.Replace("+", "%2b");
                line = HttpUtility.UrlDecode(line, new UTF8Encoding(false, true));
                if (sign == '-')
                {
                    // Deletion.
                    patch.Diffs.Add(new Diff(Operation.Delete, line));
                }
                else if (sign == '+')
                {
                    // Insertion.
                    patch.Diffs.Add(new Diff(Operation.Insert, line));
                }
                else if (sign == ' ')
                {
                    // Minor equality.
                    patch.Diffs.Add(new Diff(Operation.Equal, line));
                }
                else if (sign == '@')
                {
                    // Start of next patch.
                    break;
                }
                else
                {
                    // WTF?
                    throw new ArgumentException(
                        "Invalid patch mode '" + sign + "' in: " + line);
                }
                textPointer++;
            }
        }
        return patches;
    }

    /**
     * Unescape selected chars for compatability with JavaScript's encodeURI.
     * In speed critical applications this could be dropped since the
     * receiving application will certainly decode these fine.
     * Note that this function is case-sensitive.  Thus "%3F" would not be
     * unescaped.  But this is ok because it is only called with the output of
     * HttpUtility.UrlEncode which returns lowercase hex.
     *
     * Example: "%3f" -> "?", "%24" -> "$", etc.
     *
     * @param str The string to escape.
     * @return The escaped string.
     */
    public static string UnescapeForEncodeUriCompatability(string str)
    {
        return str.Replace("%21", "!").Replace("%7e", "~")
            .Replace("%27", "'").Replace("%28", "(").Replace("%29", ")")
            .Replace("%3b", ";").Replace("%2f", "/").Replace("%3f", "?")
            .Replace("%3a", ":").Replace("%40", "@").Replace("%26", "&")
            .Replace("%3d", "=").Replace("%2b", "+").Replace("%24", "$")
            .Replace("%2c", ",").Replace("%23", "#");
    }
}