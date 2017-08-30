/*
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
 * Diff Match and Patch -- Test Harness
 * http://code.google.com/p/google-diff-match-patch/
 */

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace DiffMatchPatchSharp.Tests
{
    [TestFixture]
    public class DiffMatchPatchTest : DiffMatchPatch
    {
        [Test]
        public void DiffCommonPrefixTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Detect any common suffix.
            // Null case.
            Assert.AreEqual(0, dmp.DiffCommonPrefix("abc", "xyz"));

            // Non-null case.
            Assert.AreEqual(4, dmp.DiffCommonPrefix("1234abcdef", "1234xyz"));

            // Whole case.
            Assert.AreEqual(4, dmp.DiffCommonPrefix("1234", "1234xyz"));
        }

        [Test]
        public void DiffCommonSuffixTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Detect any common suffix.
            // Null case.
            Assert.AreEqual(0, dmp.DiffCommonSuffix("abc", "xyz"));

            // Non-null case.
            Assert.AreEqual(4, dmp.DiffCommonSuffix("abcdef1234", "xyz1234"));

            // Whole case.
            Assert.AreEqual(4, dmp.DiffCommonSuffix("1234", "xyz1234"));
        }

        [Test]
        public void DiffCommonOverlapTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Detect any suffix/prefix overlap.
            // Null case.
            Assert.AreEqual(0, dmp.DiffCommonOverlap("", "abcd"));

            // Whole case.
            Assert.AreEqual(3, dmp.DiffCommonOverlap("abc", "abcd"));

            // No overlap.
            Assert.AreEqual(0, dmp.DiffCommonOverlap("123456", "abcd"));

            // Overlap.
            Assert.AreEqual(3, dmp.DiffCommonOverlap("123456xxx", "xxxabcd"));

            // Unicode.
            // Some overly clever languages (C#) may treat ligatures as equal to their
            // component letters.  E.g. U+FB01 == 'fi'
            Assert.AreEqual(0, dmp.DiffCommonOverlap("fi", "\ufb01i"));
        }

        [Test]
        public void DiffHalfmatchTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            dmp.DiffTimeout = 1;
            // No match.
            Assert.IsNull(dmp.DiffHalfMatch("1234567890", "abcdef"));

            Assert.IsNull(dmp.DiffHalfMatch("12345", "23"));

            // Single Match.
            CollectionAssert.AreEqual(new[] { "12", "90", "a", "z", "345678" }, dmp.DiffHalfMatch("1234567890", "a345678z"));

            CollectionAssert.AreEqual(new[] { "a", "z", "12", "90", "345678" }, dmp.DiffHalfMatch("a345678z", "1234567890"));

            CollectionAssert.AreEqual(new[] { "abc", "z", "1234", "0", "56789" }, dmp.DiffHalfMatch("abc56789z", "1234567890"));

            CollectionAssert.AreEqual(new[] { "a", "xyz", "1", "7890", "23456" }, dmp.DiffHalfMatch("a23456xyz", "1234567890"));

            // Multiple Matches.
            CollectionAssert.AreEqual(new[] { "12123", "123121", "a", "z", "1234123451234" }, dmp.DiffHalfMatch("121231234123451234123121", "a1234123451234z"));

            CollectionAssert.AreEqual(new[] { "", "-=-=-=-=-=", "x", "", "x-=-=-=-=-=-=-=" }, dmp.DiffHalfMatch("x-=-=-=-=-=-=-=-=-=-=-=-=", "xx-=-=-=-=-=-=-="));

            CollectionAssert.AreEqual(new[] { "-=-=-=-=-=", "", "", "y", "-=-=-=-=-=-=-=y" }, dmp.DiffHalfMatch("-=-=-=-=-=-=-=-=-=-=-=-=y", "-=-=-=-=-=-=-=yy"));

            // Non-optimal halfmatch.
            // Optimal diff would be -q+x=H-i+e=lloHe+Hu=llo-Hew+y not -qHillo+x=HelloHe-w+Hulloy
            CollectionAssert.AreEqual(new[] { "qHillo", "w", "x", "Hulloy", "HelloHe" }, dmp.DiffHalfMatch("qHilloHelloHew", "xHelloHeHulloy"));

            // Optimal no halfmatch.
            dmp.DiffTimeout = 0;
            Assert.IsNull(dmp.DiffHalfMatch("qHilloHelloHew", "xHelloHeHulloy"));
        }

        [Test]
        public void DiffLinesToCharsTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Convert lines down to characters.
            List<string> tmpVector = new List<string>();
            tmpVector.Add("");
            tmpVector.Add("alpha\n");
            tmpVector.Add("beta\n");
            Object[] result = dmp.DiffLinesToChars("alpha\nbeta\nalpha\n", "beta\nalpha\nbeta\n");
            Assert.AreEqual("\u0001\u0002\u0001", result[0]);
            Assert.AreEqual("\u0002\u0001\u0002", result[1]);
            CollectionAssert.AreEqual(tmpVector, (List<string>)result[2]);

            tmpVector.Clear();
            tmpVector.Add("");
            tmpVector.Add("alpha\r\n");
            tmpVector.Add("beta\r\n");
            tmpVector.Add("\r\n");
            result = dmp.DiffLinesToChars("", "alpha\r\nbeta\r\n\r\n\r\n");
            Assert.AreEqual("", result[0]);
            Assert.AreEqual("\u0001\u0002\u0003\u0003", result[1]);
            CollectionAssert.AreEqual(tmpVector, (List<string>)result[2]);

            tmpVector.Clear();
            tmpVector.Add("");
            tmpVector.Add("a");
            tmpVector.Add("b");
            result = dmp.DiffLinesToChars("a", "b");
            Assert.AreEqual("\u0001", result[0]);
            Assert.AreEqual("\u0002", result[1]);
            CollectionAssert.AreEqual(tmpVector, (List<string>)result[2]);

            // More than 256 to reveal any 8-bit limitations.
            int n = 300;
            tmpVector.Clear();
            StringBuilder lineList = new StringBuilder();
            StringBuilder charList = new StringBuilder();
            for (int x = 1; x < n + 1; x++)
            {
                tmpVector.Add(x + "\n");
                lineList.Append(x + "\n");
                charList.Append(Convert.ToChar(x));
            }
            Assert.AreEqual(n, tmpVector.Count);
            string lines = lineList.ToString();
            string chars = charList.ToString();
            Assert.AreEqual(n, chars.Length);
            tmpVector.Insert(0, "");
            result = dmp.DiffLinesToChars(lines, "");
            Assert.AreEqual(chars, result[0]);
            Assert.AreEqual("", result[1]);
            CollectionAssert.AreEqual(tmpVector, (List<string>)result[2]);
        }

        [Test]
        public void DiffCharsToLinesTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Convert chars up to lines.
            List<Diff> diffs = new List<Diff> {
          new Diff(Operation.Equal, "\u0001\u0002\u0001"),
          new Diff(Operation.Insert, "\u0002\u0001\u0002")};
            List<string> tmpVector = new List<string>();
            tmpVector.Add("");
            tmpVector.Add("alpha\n");
            tmpVector.Add("beta\n");
            dmp.DiffCharsToLines(diffs, tmpVector);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Equal, "alpha\nbeta\nalpha\n"),
          new Diff(Operation.Insert, "beta\nalpha\nbeta\n")}, diffs);

            // More than 256 to reveal any 8-bit limitations.
            int n = 300;
            tmpVector.Clear();
            StringBuilder lineList = new StringBuilder();
            StringBuilder charList = new StringBuilder();
            for (int x = 1; x < n + 1; x++)
            {
                tmpVector.Add(x + "\n");
                lineList.Append(x + "\n");
                charList.Append(Convert.ToChar(x));
            }
            Assert.AreEqual(n, tmpVector.Count);
            string lines = lineList.ToString();
            string chars = charList.ToString();
            Assert.AreEqual(n, chars.Length);
            tmpVector.Insert(0, "");
            diffs = new List<Diff> { new Diff(Operation.Delete, chars) };
            dmp.DiffCharsToLines(diffs, tmpVector);
            CollectionAssert.AreEqual(new List<Diff>
          {new Diff(Operation.Delete, lines)}, diffs);
        }

        [Test]
        public void DiffCleanupMergeTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Cleanup a messy diff.
            // Null case.
            List<Diff> diffs = new List<Diff>();
            dmp.DiffCleanupMerge(diffs);
            CollectionAssert.AreEqual(new List<Diff>(), diffs);

            // No change case.
            diffs = new List<Diff> { new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "b"), new Diff(Operation.Insert, "c") };
            dmp.DiffCleanupMerge(diffs);
            CollectionAssert.AreEqual(new List<Diff> { new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "b"), new Diff(Operation.Insert, "c") }, diffs);

            // Merge equalities.
            diffs = new List<Diff> { new Diff(Operation.Equal, "a"), new Diff(Operation.Equal, "b"), new Diff(Operation.Equal, "c") };
            dmp.DiffCleanupMerge(diffs);
            CollectionAssert.AreEqual(new List<Diff> { new Diff(Operation.Equal, "abc") }, diffs);

            // Merge deletions.
            diffs = new List<Diff> { new Diff(Operation.Delete, "a"), new Diff(Operation.Delete, "b"), new Diff(Operation.Delete, "c") };
            dmp.DiffCleanupMerge(diffs);
            CollectionAssert.AreEqual(new List<Diff> { new Diff(Operation.Delete, "abc") }, diffs);

            // Merge insertions.
            diffs = new List<Diff> { new Diff(Operation.Insert, "a"), new Diff(Operation.Insert, "b"), new Diff(Operation.Insert, "c") };
            dmp.DiffCleanupMerge(diffs);
            CollectionAssert.AreEqual(new List<Diff> { new Diff(Operation.Insert, "abc") }, diffs);

            // Merge interweave.
            diffs = new List<Diff> { new Diff(Operation.Delete, "a"), new Diff(Operation.Insert, "b"), new Diff(Operation.Delete, "c"), new Diff(Operation.Insert, "d"), new Diff(Operation.Equal, "e"), new Diff(Operation.Equal, "f") };
            dmp.DiffCleanupMerge(diffs);
            CollectionAssert.AreEqual(new List<Diff> { new Diff(Operation.Delete, "ac"), new Diff(Operation.Insert, "bd"), new Diff(Operation.Equal, "ef") }, diffs);

            // Prefix and suffix detection.
            diffs = new List<Diff> { new Diff(Operation.Delete, "a"), new Diff(Operation.Insert, "abc"), new Diff(Operation.Delete, "dc") };
            dmp.DiffCleanupMerge(diffs);
            CollectionAssert.AreEqual(new List<Diff> { new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "d"), new Diff(Operation.Insert, "b"), new Diff(Operation.Equal, "c") }, diffs);

            // Prefix and suffix detection with equalities.
            diffs = new List<Diff> { new Diff(Operation.Equal, "x"), new Diff(Operation.Delete, "a"), new Diff(Operation.Insert, "abc"), new Diff(Operation.Delete, "dc"), new Diff(Operation.Equal, "y") };
            dmp.DiffCleanupMerge(diffs);
            CollectionAssert.AreEqual(new List<Diff> { new Diff(Operation.Equal, "xa"), new Diff(Operation.Delete, "d"), new Diff(Operation.Insert, "b"), new Diff(Operation.Equal, "cy") }, diffs);

            // Slide edit left.
            diffs = new List<Diff> { new Diff(Operation.Equal, "a"), new Diff(Operation.Insert, "ba"), new Diff(Operation.Equal, "c") };
            dmp.DiffCleanupMerge(diffs);
            CollectionAssert.AreEqual(new List<Diff> { new Diff(Operation.Insert, "ab"), new Diff(Operation.Equal, "ac") }, diffs);

            // Slide edit right.
            diffs = new List<Diff> { new Diff(Operation.Equal, "c"), new Diff(Operation.Insert, "ab"), new Diff(Operation.Equal, "a") };
            dmp.DiffCleanupMerge(diffs);
            CollectionAssert.AreEqual(new List<Diff> { new Diff(Operation.Equal, "ca"), new Diff(Operation.Insert, "ba") }, diffs);

            // Slide edit left recursive.
            diffs = new List<Diff> { new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "b"), new Diff(Operation.Equal, "c"), new Diff(Operation.Delete, "ac"), new Diff(Operation.Equal, "x") };
            dmp.DiffCleanupMerge(diffs);
            CollectionAssert.AreEqual(new List<Diff> { new Diff(Operation.Delete, "abc"), new Diff(Operation.Equal, "acx") }, diffs);

            // Slide edit right recursive.
            diffs = new List<Diff> { new Diff(Operation.Equal, "x"), new Diff(Operation.Delete, "ca"), new Diff(Operation.Equal, "c"), new Diff(Operation.Delete, "b"), new Diff(Operation.Equal, "a") };
            dmp.DiffCleanupMerge(diffs);
            CollectionAssert.AreEqual(new List<Diff> { new Diff(Operation.Equal, "xca"), new Diff(Operation.Delete, "cba") }, diffs);
        }

        [Test]
        public void DiffCleanupSemanticLosslessTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Slide diffs to match logical boundaries.
            // Null case.
            List<Diff> diffs = new List<Diff>();
            dmp.DiffCleanupSemanticLossless(diffs);
            CollectionAssert.AreEqual(new List<Diff>(), diffs);

            // Blank lines.
            diffs = new List<Diff> {
          new Diff(Operation.Equal, "AAA\r\n\r\nBBB"),
          new Diff(Operation.Insert, "\r\nDDD\r\n\r\nBBB"),
          new Diff(Operation.Equal, "\r\nEEE")
      };
            dmp.DiffCleanupSemanticLossless(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Equal, "AAA\r\n\r\n"),
          new Diff(Operation.Insert, "BBB\r\nDDD\r\n\r\n"),
          new Diff(Operation.Equal, "BBB\r\nEEE")}, diffs);

            // Line boundaries.
            diffs = new List<Diff> {
          new Diff(Operation.Equal, "AAA\r\nBBB"),
          new Diff(Operation.Insert, " DDD\r\nBBB"),
          new Diff(Operation.Equal, " EEE")};
            dmp.DiffCleanupSemanticLossless(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Equal, "AAA\r\n"),
          new Diff(Operation.Insert, "BBB DDD\r\n"),
          new Diff(Operation.Equal, "BBB EEE")}, diffs);

            // Word boundaries.
            diffs = new List<Diff> {
          new Diff(Operation.Equal, "The c"),
          new Diff(Operation.Insert, "ow and the c"),
          new Diff(Operation.Equal, "at.")};
            dmp.DiffCleanupSemanticLossless(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Equal, "The "),
          new Diff(Operation.Insert, "cow and the "),
          new Diff(Operation.Equal, "cat.")}, diffs);

            // Alphanumeric boundaries.
            diffs = new List<Diff> {
          new Diff(Operation.Equal, "The-c"),
          new Diff(Operation.Insert, "ow-and-the-c"),
          new Diff(Operation.Equal, "at.")};
            dmp.DiffCleanupSemanticLossless(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Equal, "The-"),
          new Diff(Operation.Insert, "cow-and-the-"),
          new Diff(Operation.Equal, "cat.")}, diffs);

            // Hitting the start.
            diffs = new List<Diff> {
          new Diff(Operation.Equal, "a"),
          new Diff(Operation.Delete, "a"),
          new Diff(Operation.Equal, "ax")};
            dmp.DiffCleanupSemanticLossless(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Delete, "a"),
          new Diff(Operation.Equal, "aax")}, diffs);

            // Hitting the end.
            diffs = new List<Diff> {
          new Diff(Operation.Equal, "xa"),
          new Diff(Operation.Delete, "a"),
          new Diff(Operation.Equal, "a")};
            dmp.DiffCleanupSemanticLossless(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Equal, "xaa"),
          new Diff(Operation.Delete, "a")}, diffs);

            // Sentence boundaries.
            diffs = new List<Diff> {
          new Diff(Operation.Equal, "The xxx. The "),
          new Diff(Operation.Insert, "zzz. The "),
          new Diff(Operation.Equal, "yyy.")};
            dmp.DiffCleanupSemanticLossless(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Equal, "The xxx."),
          new Diff(Operation.Insert, " The zzz."),
          new Diff(Operation.Equal, " The yyy.")}, diffs);
        }

        [Test]
        public void DiffCleanupSemanticTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Cleanup semantically trivial equalities.
            // Null case.
            List<Diff> diffs = new List<Diff>();
            dmp.DiffCleanupSemantic(diffs);
            CollectionAssert.AreEqual(new List<Diff>(), diffs);

            // No elimination #1.
            diffs = new List<Diff> {
          new Diff(Operation.Delete, "ab"),
          new Diff(Operation.Insert, "cd"),
          new Diff(Operation.Equal, "12"),
          new Diff(Operation.Delete, "e")};
            dmp.DiffCleanupSemantic(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Delete, "ab"),
          new Diff(Operation.Insert, "cd"),
          new Diff(Operation.Equal, "12"),
          new Diff(Operation.Delete, "e")}, diffs);

            // No elimination #2.
            diffs = new List<Diff> {
          new Diff(Operation.Delete, "abc"),
          new Diff(Operation.Insert, "ABC"),
          new Diff(Operation.Equal, "1234"),
          new Diff(Operation.Delete, "wxyz")};
            dmp.DiffCleanupSemantic(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Delete, "abc"),
          new Diff(Operation.Insert, "ABC"),
          new Diff(Operation.Equal, "1234"),
          new Diff(Operation.Delete, "wxyz")}, diffs);

            // Simple elimination.
            diffs = new List<Diff> {
          new Diff(Operation.Delete, "a"),
          new Diff(Operation.Equal, "b"),
          new Diff(Operation.Delete, "c")};
            dmp.DiffCleanupSemantic(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Delete, "abc"),
          new Diff(Operation.Insert, "b")}, diffs);

            // Backpass elimination.
            diffs = new List<Diff> {
          new Diff(Operation.Delete, "ab"),
          new Diff(Operation.Equal, "cd"),
          new Diff(Operation.Delete, "e"),
          new Diff(Operation.Equal, "f"),
          new Diff(Operation.Insert, "g")};
            dmp.DiffCleanupSemantic(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Delete, "abcdef"),
          new Diff(Operation.Insert, "cdfg")}, diffs);

            // Multiple eliminations.
            diffs = new List<Diff> {
          new Diff(Operation.Insert, "1"),
          new Diff(Operation.Equal, "A"),
          new Diff(Operation.Delete, "B"),
          new Diff(Operation.Insert, "2"),
          new Diff(Operation.Equal, "_"),
          new Diff(Operation.Insert, "1"),
          new Diff(Operation.Equal, "A"),
          new Diff(Operation.Delete, "B"),
          new Diff(Operation.Insert, "2")};
            dmp.DiffCleanupSemantic(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Delete, "AB_AB"),
          new Diff(Operation.Insert, "1A2_1A2")}, diffs);

            // Word boundaries.
            diffs = new List<Diff> {
          new Diff(Operation.Equal, "The c"),
          new Diff(Operation.Delete, "ow and the c"),
          new Diff(Operation.Equal, "at.")};
            dmp.DiffCleanupSemantic(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Equal, "The "),
          new Diff(Operation.Delete, "cow and the "),
          new Diff(Operation.Equal, "cat.")}, diffs);

            // No overlap elimination.
            diffs = new List<Diff> {
          new Diff(Operation.Delete, "abcxx"),
          new Diff(Operation.Insert, "xxdef")};
            dmp.DiffCleanupSemantic(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Delete, "abcxx"),
          new Diff(Operation.Insert, "xxdef")}, diffs);

            // Overlap elimination.
            diffs = new List<Diff> {
          new Diff(Operation.Delete, "abcxxx"),
          new Diff(Operation.Insert, "xxxdef")};
            dmp.DiffCleanupSemantic(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Delete, "abc"),
          new Diff(Operation.Equal, "xxx"),
          new Diff(Operation.Insert, "def")}, diffs);

            // Reverse overlap elimination.
            diffs = new List<Diff> {
          new Diff(Operation.Delete, "xxxabc"),
          new Diff(Operation.Insert, "defxxx")};
            dmp.DiffCleanupSemantic(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Insert, "def"),
          new Diff(Operation.Equal, "xxx"),
          new Diff(Operation.Delete, "abc")}, diffs);

            // Two overlap eliminations.
            diffs = new List<Diff> {
          new Diff(Operation.Delete, "abcd1212"),
          new Diff(Operation.Insert, "1212efghi"),
          new Diff(Operation.Equal, "----"),
          new Diff(Operation.Delete, "A3"),
          new Diff(Operation.Insert, "3BC")};
            dmp.DiffCleanupSemantic(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Delete, "abcd"),
          new Diff(Operation.Equal, "1212"),
          new Diff(Operation.Insert, "efghi"),
          new Diff(Operation.Equal, "----"),
          new Diff(Operation.Delete, "A"),
          new Diff(Operation.Equal, "3"),
          new Diff(Operation.Insert, "BC")}, diffs);
        }

        [Test]
        public void DiffCleanupEfficiencyTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Cleanup operationally trivial equalities.
            dmp.DiffEditCost = 4;
            // Null case.
            List<Diff> diffs = new List<Diff>();
            dmp.DiffCleanupEfficiency(diffs);
            CollectionAssert.AreEqual(new List<Diff>(), diffs);

            // No elimination.
            diffs = new List<Diff> {
          new Diff(Operation.Delete, "ab"),
          new Diff(Operation.Insert, "12"),
          new Diff(Operation.Equal, "wxyz"),
          new Diff(Operation.Delete, "cd"),
          new Diff(Operation.Insert, "34")};
            dmp.DiffCleanupEfficiency(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Delete, "ab"),
          new Diff(Operation.Insert, "12"),
          new Diff(Operation.Equal, "wxyz"),
          new Diff(Operation.Delete, "cd"),
          new Diff(Operation.Insert, "34")}, diffs);

            // Four-edit elimination.
            diffs = new List<Diff> {
          new Diff(Operation.Delete, "ab"),
          new Diff(Operation.Insert, "12"),
          new Diff(Operation.Equal, "xyz"),
          new Diff(Operation.Delete, "cd"),
          new Diff(Operation.Insert, "34")};
            dmp.DiffCleanupEfficiency(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Delete, "abxyzcd"),
          new Diff(Operation.Insert, "12xyz34")}, diffs);

            // Three-edit elimination.
            diffs = new List<Diff> {
          new Diff(Operation.Insert, "12"),
          new Diff(Operation.Equal, "x"),
          new Diff(Operation.Delete, "cd"),
          new Diff(Operation.Insert, "34")};
            dmp.DiffCleanupEfficiency(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Delete, "xcd"),
          new Diff(Operation.Insert, "12x34")}, diffs);

            // Backpass elimination.
            diffs = new List<Diff> {
          new Diff(Operation.Delete, "ab"),
          new Diff(Operation.Insert, "12"),
          new Diff(Operation.Equal, "xy"),
          new Diff(Operation.Insert, "34"),
          new Diff(Operation.Equal, "z"),
          new Diff(Operation.Delete, "cd"),
          new Diff(Operation.Insert, "56")};
            dmp.DiffCleanupEfficiency(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Delete, "abxyzcd"),
          new Diff(Operation.Insert, "12xy34z56")}, diffs);

            // High cost elimination.
            dmp.DiffEditCost = 5;
            diffs = new List<Diff> {
          new Diff(Operation.Delete, "ab"),
          new Diff(Operation.Insert, "12"),
          new Diff(Operation.Equal, "wxyz"),
          new Diff(Operation.Delete, "cd"),
          new Diff(Operation.Insert, "34")};
            dmp.DiffCleanupEfficiency(diffs);
            CollectionAssert.AreEqual(new List<Diff> {
          new Diff(Operation.Delete, "abwxyzcd"),
          new Diff(Operation.Insert, "12wxyz34")}, diffs);
            dmp.DiffEditCost = 4;
        }

        [Test]
        public void DiffPrettyHtmlTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Pretty print.
            List<Diff> diffs = new List<Diff> {
          new Diff(Operation.Equal, "a\n"),
          new Diff(Operation.Delete, "<B>b</B>"),
          new Diff(Operation.Insert, "c&d")};
            Assert.AreEqual("<span>a&para;<br></span><del style=\"background:#ffe6e6;\">&lt;B&gt;b&lt;/B&gt;</del><ins style=\"background:#e6ffe6;\">c&amp;d</ins>",
                dmp.DiffPrettyHtml(diffs));
        }

        [Test]
        public void DiffTextTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Compute the source and destination texts.
            List<Diff> diffs = new List<Diff> {
          new Diff(Operation.Equal, "jump"),
          new Diff(Operation.Delete, "s"),
          new Diff(Operation.Insert, "ed"),
          new Diff(Operation.Equal, " over "),
          new Diff(Operation.Delete, "the"),
          new Diff(Operation.Insert, "a"),
          new Diff(Operation.Equal, " lazy")};
            Assert.AreEqual("jumps over the lazy", dmp.DiffText1(diffs));

            Assert.AreEqual("jumped over a lazy", dmp.DiffText2(diffs));
        }

        [Test]
        public void DiffDeltaTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Convert a diff into delta string.
            List<Diff> diffs = new List<Diff> {
          new Diff(Operation.Equal, "jump"),
          new Diff(Operation.Delete, "s"),
          new Diff(Operation.Insert, "ed"),
          new Diff(Operation.Equal, " over "),
          new Diff(Operation.Delete, "the"),
          new Diff(Operation.Insert, "a"),
          new Diff(Operation.Equal, " lazy"),
          new Diff(Operation.Insert, "old dog")};
            string text1 = dmp.DiffText1(diffs);
            Assert.AreEqual("jumps over the lazy", text1);

            string delta = dmp.DiffToDelta(diffs);
            Assert.AreEqual("=4\t-1\t+ed\t=6\t-3\t+a\t=5\t+old dog", delta);

            // Convert delta string into a diff.
            CollectionAssert.AreEqual(diffs, dmp.DiffFromDelta(text1, delta));

            // Generates error (19 < 20).
            try
            {
                dmp.DiffFromDelta(text1 + "x", delta);
                Assert.Fail("diff_fromDelta: Too long.");
            }
            catch (ArgumentException)
            {
                // Exception expected.
            }

            // Generates error (19 > 18).
            try
            {
                dmp.DiffFromDelta(text1.Substring(1), delta);
                Assert.Fail("diff_fromDelta: Too short.");
            }
            catch (ArgumentException)
            {
                // Exception expected.
            }

            // Generates error (%c3%xy invalid Unicode).
            try
            {
                dmp.DiffFromDelta("", "+%c3%xy");
                Assert.Fail("diff_fromDelta: Invalid character.");
            }
            catch (ArgumentException)
            {
                // Exception expected.
            }

            // Test deltas with special characters.
            char zero = (char)0;
            char one = (char)1;
            char two = (char)2;
            diffs = new List<Diff> {
          new Diff(Operation.Equal, "\u0680 " + zero + " \t %"),
          new Diff(Operation.Delete, "\u0681 " + one + " \n ^"),
          new Diff(Operation.Insert, "\u0682 " + two + " \\ |")};
            text1 = dmp.DiffText1(diffs);
            Assert.AreEqual("\u0680 " + zero + " \t %\u0681 " + one + " \n ^", text1);

            delta = dmp.DiffToDelta(diffs);
            // Lowercase, due to UrlEncode uses lower.
            Assert.AreEqual("=7\t-7\t+%da%82 %02 %5c %7c", delta, "diff_toDelta: Unicode.");

            CollectionAssert.AreEqual(diffs, dmp.DiffFromDelta(text1, delta), "diff_fromDelta: Unicode.");

            // Verify pool of unchanged characters.
            diffs = new List<Diff> {
          new Diff(Operation.Insert, "A-Z a-z 0-9 - _ . ! ~ * ' ( ) ; / ? : @ & = + $ , # ")};
            string text2 = dmp.DiffText2(diffs);
            Assert.AreEqual("A-Z a-z 0-9 - _ . ! ~ * \' ( ) ; / ? : @ & = + $ , # ", text2, "diff_text2: Unchanged characters.");

            delta = dmp.DiffToDelta(diffs);
            Assert.AreEqual("+A-Z a-z 0-9 - _ . ! ~ * \' ( ) ; / ? : @ & = + $ , # ", delta, "diff_toDelta: Unchanged characters.");

            // Convert delta string into a diff.
            CollectionAssert.AreEqual(diffs, dmp.DiffFromDelta("", delta), "diff_fromDelta: Unchanged characters.");
        }

        [Test]
        public void DiffXIndexTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Translate a location in text1 to text2.
            List<Diff> diffs = new List<Diff> {
          new Diff(Operation.Delete, "a"),
          new Diff(Operation.Insert, "1234"),
          new Diff(Operation.Equal, "xyz")};
            Assert.AreEqual(5, dmp.DiffXIndex(diffs, 2), "diff_xIndex: Translation on equality.");

            diffs = new List<Diff> {
          new Diff(Operation.Equal, "a"),
          new Diff(Operation.Delete, "1234"),
          new Diff(Operation.Equal, "xyz")};
            Assert.AreEqual(1, dmp.DiffXIndex(diffs, 3), "diff_xIndex: Translation on deletion.");
        }

        [Test]
        public void DiffLevenshteinTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            List<Diff> diffs = new List<Diff> {
          new Diff(Operation.Delete, "abc"),
          new Diff(Operation.Insert, "1234"),
          new Diff(Operation.Equal, "xyz")};
            Assert.AreEqual(4, dmp.DiffLevenshtein(diffs), "diff_levenshtein: Levenshtein with trailing equality.");

            diffs = new List<Diff> {
          new Diff(Operation.Equal, "xyz"),
          new Diff(Operation.Delete, "abc"),
          new Diff(Operation.Insert, "1234")};
            Assert.AreEqual(4, dmp.DiffLevenshtein(diffs), "diff_levenshtein: Levenshtein with leading equality.");

            diffs = new List<Diff> {
          new Diff(Operation.Delete, "abc"),
          new Diff(Operation.Equal, "xyz"),
          new Diff(Operation.Insert, "1234")};
            Assert.AreEqual(7, dmp.DiffLevenshtein(diffs), "diff_levenshtein: Levenshtein with middle equality.");
        }

        [Test]
        public void DiffBisectTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Normal.
            string a = "cat";
            string b = "map";
            // Since the resulting diff hasn't been normalized, it would be ok if
            // the insertion and deletion pairs are swapped.
            // If the order changes, tweak this test as required.
            List<Diff> diffs = new List<Diff> { new Diff(Operation.Delete, "c"), new Diff(Operation.Insert, "m"), new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "t"), new Diff(Operation.Insert, "p") };
            CollectionAssert.AreEqual(diffs, dmp.DiffBisect(a, b, DateTime.MaxValue));

            // Timeout.
            diffs = new List<Diff> { new Diff(Operation.Delete, "cat"), new Diff(Operation.Insert, "map") };
            CollectionAssert.AreEqual(diffs, dmp.DiffBisect(a, b, DateTime.MinValue));
        }

        [Test]
        public void DiffMainTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Perform a trivial diff.
            List<Diff> diffs = new List<Diff>();
            CollectionAssert.AreEqual(diffs, dmp.DiffMain("", "", false), "diff_main: Null case.");

            diffs = new List<Diff> { new Diff(Operation.Equal, "abc") };
            CollectionAssert.AreEqual(diffs, dmp.DiffMain("abc", "abc", false), "diff_main: Equality.");

            diffs = new List<Diff> { new Diff(Operation.Equal, "ab"), new Diff(Operation.Insert, "123"), new Diff(Operation.Equal, "c") };
            CollectionAssert.AreEqual(diffs, dmp.DiffMain("abc", "ab123c", false), "diff_main: Simple insertion.");

            diffs = new List<Diff> { new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "123"), new Diff(Operation.Equal, "bc") };
            CollectionAssert.AreEqual(diffs, dmp.DiffMain("a123bc", "abc", false), "diff_main: Simple deletion.");

            diffs = new List<Diff> { new Diff(Operation.Equal, "a"), new Diff(Operation.Insert, "123"), new Diff(Operation.Equal, "b"), new Diff(Operation.Insert, "456"), new Diff(Operation.Equal, "c") };
            CollectionAssert.AreEqual(diffs, dmp.DiffMain("abc", "a123b456c", false), "diff_main: Two insertions.");

            diffs = new List<Diff> { new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "123"), new Diff(Operation.Equal, "b"), new Diff(Operation.Delete, "456"), new Diff(Operation.Equal, "c") };
            CollectionAssert.AreEqual(diffs, dmp.DiffMain("a123b456c", "abc", false), "diff_main: Two deletions.");

            // Perform a real diff.
            // Switch off the timeout.
            dmp.DiffTimeout = 0;
            diffs = new List<Diff> { new Diff(Operation.Delete, "a"), new Diff(Operation.Insert, "b") };
            CollectionAssert.AreEqual(diffs, dmp.DiffMain("a", "b", false), "diff_main: Simple case #1.");

            diffs = new List<Diff> { new Diff(Operation.Delete, "Apple"), new Diff(Operation.Insert, "Banana"), new Diff(Operation.Equal, "s are a"), new Diff(Operation.Insert, "lso"), new Diff(Operation.Equal, " fruit.") };
            CollectionAssert.AreEqual(diffs, dmp.DiffMain("Apples are a fruit.", "Bananas are also fruit.", false), "diff_main: Simple case #2.");

            diffs = new List<Diff> { new Diff(Operation.Delete, "a"), new Diff(Operation.Insert, "\u0680"), new Diff(Operation.Equal, "x"), new Diff(Operation.Delete, "\t"), new Diff(Operation.Insert, new string(new[] { (char)0 })) };
            CollectionAssert.AreEqual(diffs, dmp.DiffMain("ax\t", "\u0680x" + (char)0, false), "diff_main: Simple case #3.");

            diffs = new List<Diff> { new Diff(Operation.Delete, "1"), new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "y"), new Diff(Operation.Equal, "b"), new Diff(Operation.Delete, "2"), new Diff(Operation.Insert, "xab") };
            CollectionAssert.AreEqual(diffs, dmp.DiffMain("1ayb2", "abxab", false), "diff_main: Overlap #1.");

            diffs = new List<Diff> { new Diff(Operation.Insert, "xaxcx"), new Diff(Operation.Equal, "abc"), new Diff(Operation.Delete, "y") };
            CollectionAssert.AreEqual(diffs, dmp.DiffMain("abcy", "xaxcxabc", false), "diff_main: Overlap #2.");

            diffs = new List<Diff> { new Diff(Operation.Delete, "ABCD"), new Diff(Operation.Equal, "a"), new Diff(Operation.Delete, "="), new Diff(Operation.Insert, "-"), new Diff(Operation.Equal, "bcd"), new Diff(Operation.Delete, "="), new Diff(Operation.Insert, "-"), new Diff(Operation.Equal, "efghijklmnopqrs"), new Diff(Operation.Delete, "EFGHIJKLMNOefg") };
            CollectionAssert.AreEqual(diffs, dmp.DiffMain("ABCDa=bcd=efghijklmnopqrsEFGHIJKLMNOefg", "a-bcd-efghijklmnopqrs", false), "diff_main: Overlap #3.");

            diffs = new List<Diff> { new Diff(Operation.Insert, " "), new Diff(Operation.Equal, "a"), new Diff(Operation.Insert, "nd"), new Diff(Operation.Equal, " [[Pennsylvania]]"), new Diff(Operation.Delete, " and [[New") };
            CollectionAssert.AreEqual(diffs, dmp.DiffMain("a [[Pennsylvania]] and [[New", " and [[Pennsylvania]]", false), "diff_main: Large equality.");

            dmp.DiffTimeout = 0.1f;  // 100ms
            string a = "`Twas brillig, and the slithy toves\nDid gyre and gimble in the wabe:\nAll mimsy were the borogoves,\nAnd the mome raths outgrabe.\n";
            string b = "I am the very model of a modern major general,\nI've information vegetable, animal, and mineral,\nI know the kings of England, and I quote the fights historical,\nFrom Marathon to Waterloo, in order categorical.\n";
            // Increase the text lengths by 1024 times to ensure a timeout.
            for (int x = 0; x < 10; x++)
            {
                a = a + a;
                b = b + b;
            }
            DateTime startTime = DateTime.Now;
            dmp.DiffMain(a, b);
            DateTime endTime = DateTime.Now;
            // Test that we took at least the timeout period.
            Assert.IsTrue(new TimeSpan(((long)(dmp.DiffTimeout * 1000)) * 10000) <= endTime - startTime);
            // Test that we didn't take forever (be forgiving).
            // Theoretically this test could fail very occasionally if the
            // OS task swaps or locks up for a second at the wrong moment.
            Assert.IsTrue(new TimeSpan(((long)(dmp.DiffTimeout * 1000)) * 10000 * 2) > endTime - startTime);
            dmp.DiffTimeout = 0;

            // Test the linemode speedup.
            // Must be long to pass the 100 char cutoff.
            a = "1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n";
            b = "abcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\n";
            CollectionAssert.AreEqual(dmp.DiffMain(a, b, true), dmp.DiffMain(a, b, false), "diff_main: Simple line-mode.");

            a = "1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890";
            b = "abcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghij";
            CollectionAssert.AreEqual(dmp.DiffMain(a, b, true), dmp.DiffMain(a, b, false), "diff_main: Single line-mode.");

            a = "1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n";
            b = "abcdefghij\n1234567890\n1234567890\n1234567890\nabcdefghij\n1234567890\n1234567890\n1234567890\nabcdefghij\n1234567890\n1234567890\n1234567890\nabcdefghij\n";
            string[] textsLinemode = DiffRebuildTexts(dmp.DiffMain(a, b, true));
            string[] textsTextmode = DiffRebuildTexts(dmp.DiffMain(a, b, false));
            CollectionAssert.AreEqual(textsTextmode, textsLinemode, "diff_main: Overlap line-mode.");

            // Test null inputs -- not needed because nulls can't be passed in C#.
        }

        [Test]
        public void MatchAlphabetTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Initialise the bitmasks for Bitap.
            Dictionary<char, int> bitmask = new Dictionary<char, int>();
            bitmask.Add('a', 4); bitmask.Add('b', 2); bitmask.Add('c', 1);
            CollectionAssert.AreEqual(bitmask, dmp.MatchAlphabet("abc"), "match_alphabet: Unique.");

            bitmask.Clear();
            bitmask.Add('a', 37); bitmask.Add('b', 18); bitmask.Add('c', 8);
            CollectionAssert.AreEqual(bitmask, dmp.MatchAlphabet("abcaba"), "match_alphabet: Duplicates.");
        }

        [Test]
        public void MatchBitapTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();

            // Bitap algorithm.
            dmp.MatchDistance = 100;
            dmp.MatchThreshold = 0.5f;
            Assert.AreEqual(5, dmp.MatchBitap("abcdefghijk", "fgh", 5), "match_bitap: Exact match #1.");

            Assert.AreEqual(5, dmp.MatchBitap("abcdefghijk", "fgh", 0), "match_bitap: Exact match #2.");

            Assert.AreEqual(4, dmp.MatchBitap("abcdefghijk", "efxhi", 0), "match_bitap: Fuzzy match #1.");

            Assert.AreEqual(2, dmp.MatchBitap("abcdefghijk", "cdefxyhijk", 5), "match_bitap: Fuzzy match #2.");

            Assert.AreEqual(-1, dmp.MatchBitap("abcdefghijk", "bxy", 1), "match_bitap: Fuzzy match #3.");

            Assert.AreEqual(2, dmp.MatchBitap("123456789xx0", "3456789x0", 2), "match_bitap: Overflow.");

            Assert.AreEqual(0, dmp.MatchBitap("abcdef", "xxabc", 4), "match_bitap: Before start match.");

            Assert.AreEqual(3, dmp.MatchBitap("abcdef", "defyy", 4), "match_bitap: Beyond end match.");

            Assert.AreEqual(0, dmp.MatchBitap("abcdef", "xabcdefy", 0), "match_bitap: Oversized pattern.");

            dmp.MatchThreshold = 0.4f;
            Assert.AreEqual(4, dmp.MatchBitap("abcdefghijk", "efxyhi", 1), "match_bitap: Threshold #1.");

            dmp.MatchThreshold = 0.3f;
            Assert.AreEqual(-1, dmp.MatchBitap("abcdefghijk", "efxyhi", 1), "match_bitap: Threshold #2.");

            dmp.MatchThreshold = 0.0f;
            Assert.AreEqual(1, dmp.MatchBitap("abcdefghijk", "bcdef", 1), "match_bitap: Threshold #3.");

            dmp.MatchThreshold = 0.5f;
            Assert.AreEqual(0, dmp.MatchBitap("abcdexyzabcde", "abccde", 3), "match_bitap: Multiple select #1.");

            Assert.AreEqual(8, dmp.MatchBitap("abcdexyzabcde", "abccde", 5), "match_bitap: Multiple select #2.");

            dmp.MatchDistance = 10;  // Strict location.
            Assert.AreEqual(-1, dmp.MatchBitap("abcdefghijklmnopqrstuvwxyz", "abcdefg", 24), "match_bitap: Distance test #1.");

            Assert.AreEqual(0, dmp.MatchBitap("abcdefghijklmnopqrstuvwxyz", "abcdxxefg", 1), "match_bitap: Distance test #2.");

            dmp.MatchDistance = 1000;  // Loose location.
            Assert.AreEqual(0, dmp.MatchBitap("abcdefghijklmnopqrstuvwxyz", "abcdefg", 24), "match_bitap: Distance test #3.");
        }

        [Test]
        public void MatchMainTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            // Full match.
            Assert.AreEqual(0, dmp.MatchMain("abcdef", "abcdef", 1000), "match_main: Equality.");

            Assert.AreEqual(-1, dmp.MatchMain("", "abcdef", 1), "match_main: Null text.");

            Assert.AreEqual(3, dmp.MatchMain("abcdef", "", 3), "match_main: Null pattern.");

            Assert.AreEqual(3, dmp.MatchMain("abcdef", "de", 3), "match_main: Exact match.");

            Assert.AreEqual(3, dmp.MatchMain("abcdef", "defy", 4), "match_main: Beyond end match.");

            Assert.AreEqual(0, dmp.MatchMain("abcdef", "abcdefy", 0), "match_main: Oversized pattern.");

            dmp.MatchThreshold = 0.7f;
            Assert.AreEqual(4, dmp.MatchMain("I am the very model of a modern major general.", " that berry ", 5), "match_main: Complex match.");
            dmp.MatchThreshold = 0.5f;

            // Test null inputs -- not needed because nulls can't be passed in C#.
        }

        [Test]
        public void PatchPatchObjTest()
        {
            // Patch Object.
            Patch p = new Patch();
            p.Start1 = 20;
            p.Start2 = 21;
            p.Length1 = 18;
            p.Length2 = 17;
            p.Diffs = new List<Diff> {
              new Diff(Operation.Equal, "jump"),
              new Diff(Operation.Delete, "s"),
              new Diff(Operation.Insert, "ed"),
              new Diff(Operation.Equal, " over "),
              new Diff(Operation.Delete, "the"),
              new Diff(Operation.Insert, "a"),
              new Diff(Operation.Equal, "\nlaz")};
            string strp = "@@ -21,18 +22,17 @@\n jump\n-s\n+ed\n  over \n-the\n+a\n %0alaz\n";
            Assert.AreEqual(strp, p.ToString(), "Patch: toString.");
        }

        [Test]
        public void PatchFromTextTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            Assert.IsTrue(dmp.PatchFromText("").Count == 0, "patch_fromText: #0.");

            string strp = "@@ -21,18 +22,17 @@\n jump\n-s\n+ed\n  over \n-the\n+a\n %0alaz\n";
            Assert.AreEqual(strp, dmp.PatchFromText(strp)[0].ToString(), "patch_fromText: #1.");

            Assert.AreEqual("@@ -1 +1 @@\n-a\n+b\n", dmp.PatchFromText("@@ -1 +1 @@\n-a\n+b\n")[0].ToString(), "patch_fromText: #2.");

            Assert.AreEqual("@@ -1,3 +0,0 @@\n-abc\n", dmp.PatchFromText("@@ -1,3 +0,0 @@\n-abc\n")[0].ToString(), "patch_fromText: #3.");

            Assert.AreEqual("@@ -0,0 +1,3 @@\n+abc\n", dmp.PatchFromText("@@ -0,0 +1,3 @@\n+abc\n")[0].ToString(), "patch_fromText: #4.");

            // Generates error.
            try
            {
                dmp.PatchFromText("Bad\nPatch\n");
                Assert.Fail("patch_fromText: #5.");
            }
            catch (ArgumentException)
            {
                // Exception expected.
            }
        }

        [Test]
        public void PatchToTextTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            string strp = "@@ -21,18 +22,17 @@\n jump\n-s\n+ed\n  over \n-the\n+a\n  laz\n";
            List<Patch> patches;
            patches = dmp.PatchFromText(strp);
            string result = dmp.PatchToText(patches);
            Assert.AreEqual(strp, result);

            strp = "@@ -1,9 +1,9 @@\n-f\n+F\n oo+fooba\n@@ -7,9 +7,9 @@\n obar\n-,\n+.\n  tes\n";
            patches = dmp.PatchFromText(strp);
            result = dmp.PatchToText(patches);
            Assert.AreEqual(strp, result);
        }

        [Test]
        public void PatchAddContextTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            dmp.PatchMargin = 4;
            Patch p;
            p = dmp.PatchFromText("@@ -21,4 +21,10 @@\n-jump\n+somersault\n")[0];
            dmp.PatchAddContext(p, "The quick brown fox jumps over the lazy dog.");
            Assert.AreEqual("@@ -17,12 +17,18 @@\n fox \n-jump\n+somersault\n s ov\n", p.ToString(), "patch_addContext: Simple case.");

            p = dmp.PatchFromText("@@ -21,4 +21,10 @@\n-jump\n+somersault\n")[0];
            dmp.PatchAddContext(p, "The quick brown fox jumps.");
            Assert.AreEqual("@@ -17,10 +17,16 @@\n fox \n-jump\n+somersault\n s.\n", p.ToString(), "patch_addContext: Not enough trailing context.");

            p = dmp.PatchFromText("@@ -3 +3,2 @@\n-e\n+at\n")[0];
            dmp.PatchAddContext(p, "The quick brown fox jumps.");
            Assert.AreEqual("@@ -1,7 +1,8 @@\n Th\n-e\n+at\n  qui\n", p.ToString(), "patch_addContext: Not enough leading context.");

            p = dmp.PatchFromText("@@ -3 +3,2 @@\n-e\n+at\n")[0];
            dmp.PatchAddContext(p, "The quick brown fox jumps.  The quick brown fox crashes.");
            Assert.AreEqual("@@ -1,27 +1,28 @@\n Th\n-e\n+at\n  quick brown fox jumps. \n", p.ToString(), "patch_addContext: Ambiguity.");
        }

        [Test]
        public void PatchMakeTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            List<Patch> patches;
            patches = dmp.PatchMake("", "");
            Assert.AreEqual("", dmp.PatchToText(patches), "patch_make: Null case.");

            string text1 = "The quick brown fox jumps over the lazy dog.";
            string text2 = "That quick brown fox jumped over a lazy dog.";
            string expectedPatch = "@@ -1,8 +1,7 @@\n Th\n-at\n+e\n  qui\n@@ -21,17 +21,18 @@\n jump\n-ed\n+s\n  over \n-a\n+the\n  laz\n";
            // The second patch must be "-21,17 +21,18", not "-22,17 +21,18" due to rolling context.
            patches = dmp.PatchMake(text2, text1);
            Assert.AreEqual(expectedPatch, dmp.PatchToText(patches), "patch_make: Text2+Text1 inputs.");

            expectedPatch = "@@ -1,11 +1,12 @@\n Th\n-e\n+at\n  quick b\n@@ -22,18 +22,17 @@\n jump\n-s\n+ed\n  over \n-the\n+a\n  laz\n";
            patches = dmp.PatchMake(text1, text2);
            Assert.AreEqual(expectedPatch, dmp.PatchToText(patches), "patch_make: Text1+Text2 inputs.");

            List<Diff> diffs = dmp.DiffMain(text1, text2, false);
            patches = dmp.PatchMake(diffs);
            Assert.AreEqual(expectedPatch, dmp.PatchToText(patches), "patch_make: Diff input.");

            patches = dmp.PatchMake(text1, diffs);
            Assert.AreEqual(expectedPatch, dmp.PatchToText(patches), "patch_make: Text1+Diff inputs.");

            patches = dmp.PatchMake(text1, text2, diffs);
            Assert.AreEqual(expectedPatch, dmp.PatchToText(patches), "patch_make: Text1+Text2+Diff inputs (deprecated).");

            patches = dmp.PatchMake("`1234567890-=[]\\;',./", "~!@#$%^&*()_+{}|:\"<>?");
            Assert.AreEqual("@@ -1,21 +1,21 @@\n-%601234567890-=%5b%5d%5c;',./\n+~!@#$%25%5e&*()_+%7b%7d%7c:%22%3c%3e?\n",
                dmp.PatchToText(patches),
                "patch_toText: Character encoding.");

            diffs = new List<Diff> {
          new Diff(Operation.Delete, "`1234567890-=[]\\;',./"),
          new Diff(Operation.Insert, "~!@#$%^&*()_+{}|:\"<>?")};
            CollectionAssert.AreEqual(diffs,
                dmp.PatchFromText("@@ -1,21 +1,21 @@\n-%601234567890-=%5B%5D%5C;',./\n+~!@#$%25%5E&*()_+%7B%7D%7C:%22%3C%3E?\n")[0].Diffs,
                "patch_fromText: Character decoding.");

            text1 = "";
            for (int x = 0; x < 100; x++)
            {
                text1 += "abcdef";
            }
            text2 = text1 + "123";
            expectedPatch = "@@ -573,28 +573,31 @@\n cdefabcdefabcdefabcdefabcdef\n+123\n";
            patches = dmp.PatchMake(text1, text2);
            Assert.AreEqual(expectedPatch, dmp.PatchToText(patches), "patch_make: Long string with repeats.");

            // Test null inputs -- not needed because nulls can't be passed in C#.
        }

        [Test]
        public void PatchSplitMaxTest()
        {
            // Assumes that Match_MaxBits is 32.
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            List<Patch> patches;

            patches = dmp.PatchMake("abcdefghijklmnopqrstuvwxyz01234567890", "XabXcdXefXghXijXklXmnXopXqrXstXuvXwxXyzX01X23X45X67X89X0");
            dmp.PatchSplitMax(patches);
            Assert.AreEqual("@@ -1,32 +1,46 @@\n+X\n ab\n+X\n cd\n+X\n ef\n+X\n gh\n+X\n ij\n+X\n kl\n+X\n mn\n+X\n op\n+X\n qr\n+X\n st\n+X\n uv\n+X\n wx\n+X\n yz\n+X\n 012345\n@@ -25,13 +39,18 @@\n zX01\n+X\n 23\n+X\n 45\n+X\n 67\n+X\n 89\n+X\n 0\n", dmp.PatchToText(patches));

            patches = dmp.PatchMake("abcdef1234567890123456789012345678901234567890123456789012345678901234567890uvwxyz", "abcdefuvwxyz");
            string oldToText = dmp.PatchToText(patches);
            dmp.PatchSplitMax(patches);
            Assert.AreEqual(oldToText, dmp.PatchToText(patches));

            patches = dmp.PatchMake("1234567890123456789012345678901234567890123456789012345678901234567890", "abc");
            dmp.PatchSplitMax(patches);
            Assert.AreEqual("@@ -1,32 +1,4 @@\n-1234567890123456789012345678\n 9012\n@@ -29,32 +1,4 @@\n-9012345678901234567890123456\n 7890\n@@ -57,14 +1,3 @@\n-78901234567890\n+abc\n", dmp.PatchToText(patches));

            patches = dmp.PatchMake("abcdefghij , h : 0 , t : 1 abcdefghij , h : 0 , t : 1 abcdefghij , h : 0 , t : 1", "abcdefghij , h : 1 , t : 1 abcdefghij , h : 1 , t : 1 abcdefghij , h : 0 , t : 1");
            dmp.PatchSplitMax(patches);
            Assert.AreEqual("@@ -2,32 +2,32 @@\n bcdefghij , h : \n-0\n+1\n  , t : 1 abcdef\n@@ -29,32 +29,32 @@\n bcdefghij , h : \n-0\n+1\n  , t : 1 abcdef\n", dmp.PatchToText(patches));
        }

        [Test]
        public void PatchAddPaddingTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            List<Patch> patches;
            patches = dmp.PatchMake("", "test");
            Assert.AreEqual("@@ -0,0 +1,4 @@\n+test\n",
               dmp.PatchToText(patches),
               "patch_addPadding: Both edges full.");
            dmp.PatchAddPadding(patches);
            Assert.AreEqual("@@ -1,8 +1,12 @@\n %01%02%03%04\n+test\n %01%02%03%04\n",
                dmp.PatchToText(patches),
                "patch_addPadding: Both edges full.");

            patches = dmp.PatchMake("XY", "XtestY");
            Assert.AreEqual("@@ -1,2 +1,6 @@\n X\n+test\n Y\n",
                dmp.PatchToText(patches),
                "patch_addPadding: Both edges partial.");
            dmp.PatchAddPadding(patches);
            Assert.AreEqual("@@ -2,8 +2,12 @@\n %02%03%04X\n+test\n Y%01%02%03\n",
                dmp.PatchToText(patches),
                "patch_addPadding: Both edges partial.");

            patches = dmp.PatchMake("XXXXYYYY", "XXXXtestYYYY");
            Assert.AreEqual("@@ -1,8 +1,12 @@\n XXXX\n+test\n YYYY\n",
                dmp.PatchToText(patches),
                "patch_addPadding: Both edges none.");
            dmp.PatchAddPadding(patches);
            Assert.AreEqual("@@ -5,8 +5,12 @@\n XXXX\n+test\n YYYY\n",
               dmp.PatchToText(patches),
               "patch_addPadding: Both edges none.");
        }

        [Test]
        public void PatchApplyTest()
        {
            DiffMatchPatchTest dmp = new DiffMatchPatchTest();
            dmp.MatchDistance = 1000;
            dmp.MatchThreshold = 0.5f;
            dmp.PatchDeleteThreshold = 0.5f;
            List<Patch> patches;
            patches = dmp.PatchMake("", "");
            Object[] results = dmp.PatchApply(patches, "Hello world.");
            bool[] boolArray = (bool[])results[1];
            string resultStr = results[0] + "\t" + boolArray.Length;
            Assert.AreEqual("Hello world.\t0", resultStr, "patch_apply: Null case.");

            patches = dmp.PatchMake("The quick brown fox jumps over the lazy dog.", "That quick brown fox jumped over a lazy dog.");
            results = dmp.PatchApply(patches, "The quick brown fox jumps over the lazy dog.");
            boolArray = (bool[])results[1];
            resultStr = results[0] + "\t" + boolArray[0] + "\t" + boolArray[1];
            Assert.AreEqual("That quick brown fox jumped over a lazy dog.\tTrue\tTrue", resultStr, "patch_apply: Exact match.");

            results = dmp.PatchApply(patches, "The quick red rabbit jumps over the tired tiger.");
            boolArray = (bool[])results[1];
            resultStr = results[0] + "\t" + boolArray[0] + "\t" + boolArray[1];
            Assert.AreEqual("That quick red rabbit jumped over a tired tiger.\tTrue\tTrue", resultStr, "patch_apply: Partial match.");

            results = dmp.PatchApply(patches, "I am the very model of a modern major general.");
            boolArray = (bool[])results[1];
            resultStr = results[0] + "\t" + boolArray[0] + "\t" + boolArray[1];
            Assert.AreEqual("I am the very model of a modern major general.\tFalse\tFalse", resultStr, "patch_apply: Failed match.");

            patches = dmp.PatchMake("x1234567890123456789012345678901234567890123456789012345678901234567890y", "xabcy");
            results = dmp.PatchApply(patches, "x123456789012345678901234567890-----++++++++++-----123456789012345678901234567890y");
            boolArray = (bool[])results[1];
            resultStr = results[0] + "\t" + boolArray[0] + "\t" + boolArray[1];
            Assert.AreEqual("xabcy\tTrue\tTrue", resultStr, "patch_apply: Big delete, small change.");

            patches = dmp.PatchMake("x1234567890123456789012345678901234567890123456789012345678901234567890y", "xabcy");
            results = dmp.PatchApply(patches, "x12345678901234567890---------------++++++++++---------------12345678901234567890y");
            boolArray = (bool[])results[1];
            resultStr = results[0] + "\t" + boolArray[0] + "\t" + boolArray[1];
            Assert.AreEqual("xabc12345678901234567890---------------++++++++++---------------12345678901234567890y\tFalse\tTrue", resultStr, "patch_apply: Big delete, big change 1.");

            dmp.PatchDeleteThreshold = 0.6f;
            patches = dmp.PatchMake("x1234567890123456789012345678901234567890123456789012345678901234567890y", "xabcy");
            results = dmp.PatchApply(patches, "x12345678901234567890---------------++++++++++---------------12345678901234567890y");
            boolArray = (bool[])results[1];
            resultStr = results[0] + "\t" + boolArray[0] + "\t" + boolArray[1];
            Assert.AreEqual("xabcy\tTrue\tTrue", resultStr, "patch_apply: Big delete, big change 2.");
            dmp.PatchDeleteThreshold = 0.5f;

            dmp.MatchThreshold = 0.0f;
            dmp.MatchDistance = 0;
            patches = dmp.PatchMake("abcdefghijklmnopqrstuvwxyz--------------------1234567890", "abcXXXXXXXXXXdefghijklmnopqrstuvwxyz--------------------1234567YYYYYYYYYY890");
            results = dmp.PatchApply(patches, "ABCDEFGHIJKLMNOPQRSTUVWXYZ--------------------1234567890");
            boolArray = (bool[])results[1];
            resultStr = results[0] + "\t" + boolArray[0] + "\t" + boolArray[1];
            Assert.AreEqual("ABCDEFGHIJKLMNOPQRSTUVWXYZ--------------------1234567YYYYYYYYYY890\tFalse\tTrue", resultStr, "patch_apply: Compensate for failed patch.");
            dmp.MatchThreshold = 0.5f;
            dmp.MatchDistance = 1000;

            patches = dmp.PatchMake("", "test");
            string patchStr = dmp.PatchToText(patches);
            dmp.PatchApply(patches, "");
            Assert.AreEqual(patchStr, dmp.PatchToText(patches), "patch_apply: No side effects.");

            patches = dmp.PatchMake("The quick brown fox jumps over the lazy dog.", "Woof");
            patchStr = dmp.PatchToText(patches);
            dmp.PatchApply(patches, "The quick brown fox jumps over the lazy dog.");
            Assert.AreEqual(patchStr, dmp.PatchToText(patches), "patch_apply: No side effects with major delete.");

            patches = dmp.PatchMake("", "test");
            results = dmp.PatchApply(patches, "");
            boolArray = (bool[])results[1];
            resultStr = results[0] + "\t" + boolArray[0];
            Assert.AreEqual("test\tTrue", resultStr, "patch_apply: Edge exact match.");

            patches = dmp.PatchMake("XY", "XtestY");
            results = dmp.PatchApply(patches, "XY");
            boolArray = (bool[])results[1];
            resultStr = results[0] + "\t" + boolArray[0];
            Assert.AreEqual("XtestY\tTrue", resultStr, "patch_apply: Near edge exact match.");

            patches = dmp.PatchMake("y", "y123");
            results = dmp.PatchApply(patches, "x");
            boolArray = (bool[])results[1];
            resultStr = results[0] + "\t" + boolArray[0];
            Assert.AreEqual("x123\tTrue", resultStr, "patch_apply: Edge partial match.");
        }

        private static string[] DiffRebuildTexts(List<Diff> diffs)
        {
            string[] text = { "", "" };
            foreach (Diff myDiff in diffs)
            {
                if (myDiff.Operation != Operation.Insert)
                {
                    text[0] += myDiff.Text;
                }
                if (myDiff.Operation != Operation.Delete)
                {
                    text[1] += myDiff.Text;
                }
            }
            return text;
        }
    }
}
