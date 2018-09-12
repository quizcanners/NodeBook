﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharedTools_Stuff;
using STD_Logic;
using System;
using PlayerAndEditorGUI;

namespace NodeNotes
{
    public class SavedProgress: AbstractKeepUnrecognized_STD, IGotName {

        public string userName = "Unknown";
        public List<BookMark> bookMarks = new List<BookMark>();
        public bool isADeveloper = false;

        static Node _currentNode;

        public Node CurrentNode
        {
            get => _currentNode;
            set  {

                if (_currentNode != null && _currentNode.root != value.root)
                    SetNextBook(value.root);

                _currentNode = value;

            }
        }

        public string NameForPEGI { get => userName; set => userName = value; }

        public void ExitCurrentBook() {
            if (bookMarks.Count == 0) {
                Debug.LogError("Can't Exit book because it was the Start of the journey.");
                return;
            }

            var lastB = bookMarks.Last().bookName;

            var b = Shortcuts.TryGetBook(lastB);

            if (b == null) {
                Debug.LogError("No {0} book found".F(lastB));
                return;
            }

            SetNextBook(b);
        }

        void SetNextBook (NodeBook nextBook) {

            if (nextBook == null)
                Debug.LogError("Next book is null");

            var bMarkForNextBook = bookMarks.GetByIGotName(nextBook.NameForPEGI);

            if (bMarkForNextBook == null)  {

                var currentBook = _currentNode.root;

                if (currentBook != null)  {

                    var bm = new BookMark()  {
                        bookName = currentBook.NameForPEGI,
                        nodeIndex = _currentNode.IndexForPEGI,
                        values = Values.global.Encode().ToString()
                    };
                }
            }
            else  {
                int ind = bookMarks.IndexOf(bMarkForNextBook);
                bookMarks = bookMarks.GetRange(0, ind);

                var bNode = nextBook.allBaseNodes[bMarkForNextBook.nodeIndex];

                if (bNode == null)
                    Debug.LogError("No node {0} in {1}".F(bMarkForNextBook.nodeIndex, bMarkForNextBook.bookName));
                else {
                    var n = bNode as Node;
                    if (n == null)
                        Debug.LogError("Node_Base {0} is note a Node ".F(bMarkForNextBook.nodeIndex));
                    else
                        _currentNode = n;
                }
            }
        }

        #region Inspector

        int editedMark = -1;
        public override bool PEGI() {

            bool changed = base.PEGI();

            if (!isADeveloper && "Make A Developer".Click().nl())
                isADeveloper = true;

            if ((isADeveloper || Application.isEditor) && "Make a user".Click().nl())
                isADeveloper = false;

            "Marks ".edit_List(bookMarks,ref editedMark);

            return changed;
        }

        #endregion

        #region Encoding_Decoding

        public override StdEncoder Encode() {
            var cody = this.EncodeUnrecognized()
            .Add("bm", bookMarks)
            .Add("vals", Values.global)
            .Add_Bool("dev", isADeveloper)
            .Add_String("n", userName);

            var cur = Shortcuts.CurrentNode;
            if (cur != null) {
                cody.Add_String("curB", cur.root.NameForPEGI)
                .Add("cur", cur.IndexForPEGI);
            }

            return cody;
        }

        public override ISTD Decode(string data)
        {
            var ret = base.Decode(data);

            var b = Shortcuts.TryGetBook(tmpBook);

            if (b != null)
                Shortcuts.CurrentNode = b.allBaseNodes[tmpNode] as Node;

            return ret;
        }

        static string tmpBook;
        static int tmpNode;

        public override bool Decode(string tag, string data) {
           switch (tag) {
                case "bm": data.DecodeInto(out bookMarks); break;
                case "vals": data.DecodeInto(out Values.global); break;
                case "cur": tmpNode = data.ToInt(); break;
                case "curB": tmpBook = data; break;
                case "dev": isADeveloper = data.ToBool(); break;
                case "n": userName = data; break;
                default: return false;
           }

           return true;
        }

        #endregion

    }
}
