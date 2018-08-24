﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharedTools_Stuff;
using System;
using STD_Logic;
using PlayerAndEditorGUI;

namespace LinkedNotes
{
    [CreateAssetMenu(fileName = "Story Shortcuts", menuName ="Story Nodes/Shortcuts", order = 0)]
    public class Shortcuts : STD_ReferancesHolder, IKeepMySTD
    {

        [NonSerialized] public static List<NodeBook> books = new List<NodeBook>(); // Shortcuts or a complete books

        public static int playingInBook;
        public static int playingInNode;

        public static Node TryGetCurrentNode()
        {
            var book = books.TryGet(playingInBook);
            if (book != null)
            {
                var node = book.allBaseNodes[playingInNode];
                if (node == null)
                    Debug.Log("Node is null");
                else
                {
                    var nn = node as Node;
                    if (nn == null)
                        Debug.Log("Node can't hold subnodes");
                    else
                        return nn;
                }
                return null;
            }
            else
                Debug.Log("Book is null");

            return null;
        }

        [HideInInspector]
        [SerializeField]
        string std_Data = "";
        public string Config_STD {
            get { return std_Data; }
            set { std_Data = value; }
        }

        int inspectedBook = -1;

        public override bool PEGI()
        {
            bool changed = false;

            "Shortcuts".nl();

            "Active B:{0} N:{1} = {2}".F(playingInBook, playingInNode, TryGetCurrentNode().ToPEGIstring()).nl();

            if (inspectedBook == -1)
                changed |= base.PEGI().nl();
            else
                showDebug = false;

            if (!showDebug)
                "Books ".edit_List(books, ref inspectedBook, true);
            
            return changed;
        }
        
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("vals", Values.global, this)
            .Add("trigs", TriggerGroup.all)
            .Add("books", books, this)
            .Add("pb", playingInBook)
            .Add("pn", playingInNode);

        public override ISTD Decode(string data)
        {
            var ret = base.Decode(data);

            for (int i = 0; i < books.Count; i++)
                books[i].IndexForPEGI = i;

            return ret;
        }

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "vals": data.DecodeInto(out Values.global, this); break;
                case "trigs": data.DecodeInto(out TriggerGroup.all); break;
                case "books": data.DecodeInto(out books, this); break;
                case "pb": playingInBook = data.ToInt(); break;
                case "pn": playingInNode = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
    }
}