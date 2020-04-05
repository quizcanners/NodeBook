﻿using System;
using System.Collections.Generic;
using System.IO;
using NodeNotes_Visual;
using PlayerAndEditorGUI;
using PlaytimePainter;
using QcTriggerLogic;
using QuizCannersUtilities;
using UnityEditor;
using UnityEngine;
using static NodeNotes.NodeNotesAssetGroups;

namespace NodeNotes {

    [CreateAssetMenu(fileName = "Story Shortcuts", menuName ="Story Nodes/Shortcuts", order = 0)]
    public class Shortcuts : CfgReferencesHolder {

        public const string ProjectName = "NodeNotes";

        public static bool editingNodes = false;

        public static bool SaveAnyChanges => user.isADeveloper;

        #region Assets

        public static Shortcuts Instance => NodesVisualLayerAbstract.InstAsNodesVisualLayer.shortcuts;

        [SerializeField] public List<NodeNotesAssetGroups> assetGroups;
        
        // MESHES
        [SerializeField] protected Mesh _defaultMesh;
        public Mesh GetMesh(string name) => _defaultMesh;
        
        // MATERIALS
        [Serializable]
        private class FilteredMaterials : FilteredAssetGroup<Material>
        {
            public FilteredMaterials(Func<NodeNotesAssetGroups, TaggedAssetsList<Material>> getOne) : base(getOne) { }
        }

        [SerializeField] private FilteredMaterials Materials = new FilteredMaterials((NodeNotesAssetGroups grp) => { return grp.materials; });
        public bool Get(string key, out Material mat)
        {
            mat = Materials.Get(key, assetGroups);
            return mat;
        }
        
        // SDF Objects
        [Serializable]
        public class FilteredSdfObjects : FilteredAssetGroup<SDFobject>
        {
            public FilteredSdfObjects(Func<NodeNotesAssetGroups, TaggedAssetsList<SDFobject>> getOne) : base(getOne) { }
        }

        public bool Get(string key, out SDFobject sdf)
        {
            sdf = SdfObjects.Get(key, assetGroups);
            return sdf;
        }

        public List<string> GetSdfObjectsKeys() => SdfObjects.GetAllKeysKeys(assetGroups);

        private FilteredSdfObjects SdfObjects = new FilteredSdfObjects((NodeNotesAssetGroups grp) => { return grp.sdfObjects; });
        

        [Serializable]
        public abstract class FilteredAssetGroup<T> where T: UnityEngine.Object
        {
            [SerializeField] protected T _defaultMaterial;
            [NonSerialized] private Dictionary<string, T> filteredMaterials = new Dictionary<string, T>();
            [NonSerialized] private List<string> allMaterialKeys;

            private Func<NodeNotesAssetGroups, TaggedAssetsList<T>> _getOne;

            public List<string> GetAllKeysKeys(List<NodeNotesAssetGroups> assetGroups)
            {
                if (allMaterialKeys != null)
                    return allMaterialKeys;

                allMaterialKeys = new List<string>();

                foreach (var assetGroup in assetGroups)
                foreach (var taggedMaterial in assetGroup.materials.taggedList)
                    allMaterialKeys.Add(taggedMaterial.tag);

                return allMaterialKeys;
            }

            public T Get(string key, List<NodeNotesAssetGroups> assetGroups)
            {
                if (key.IsNullOrEmpty())
                    return _defaultMaterial;

                T mat;

                if (!filteredMaterials.TryGetValue(key, out mat))
                    foreach (var group in assetGroups)
                        if (_getOne(group).TreGet(key, out mat))
                        {
                            filteredMaterials[key] = mat;
                            break;
                        }

                return mat ? mat : _defaultMaterial;
            }

            public FilteredAssetGroup(Func<NodeNotesAssetGroups, TaggedAssetsList<T>> getOne)
            {
                _getOne = getOne;
            }

        }
        

        [SerializeField] public AudioClip onMouseDownButtonSound;
        [SerializeField] public AudioClip onMouseClickSound;
        [SerializeField] public AudioClip onMouseClickFailedSound;
        [SerializeField] public AudioClip onMouseLeaveSound;
        [SerializeField] public AudioClip onMouseHoldSound;
        [SerializeField] public AudioClip onSwipeSound;

        #endregion

        #region Progress

        static LoopLock loopLock = new LoopLock();

        public static bool TryExitCurrentNode()
        {
            var node = CurrentNode;
            if (node != null && node.parentNode != null)
            {
                CurrentNode = node.parentNode;
                return true;
            }

            return false;
        }

        public static Node CurrentNode
        {

            get { return user.CurrentNode; }

            set   {

                if (Application.isPlaying && visualLayer && loopLock.Unlocked)
                {
                    using (loopLock.Lock()) {
                        expectingLoopCall = true;

                        try
                        {
                            visualLayer.OnNodeSet(value);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                        }

                        if (expectingLoopCall) {
                            expectingLoopCall = false;
                            user.CurrentNode = value;
                        }
                    }
                }
                else
                {
                    expectingLoopCall = false;
                    user.CurrentNode = value;
                }

            }
        }

        static bool expectingLoopCall;

        public static NodesVisualLayerAbstract visualLayer;

        [NonSerialized] public static Base_Node Cut_Paste;

        #endregion

        #region Users

        [NonSerialized] public static List<string> users = new List<string>();

        public static CurrentUser user = new CurrentUser();

        static readonly string _usersFolder = "Users";

        void LoadUser(string uname) {

            user = new CurrentUser();
            user.Decode(QcFile.Load.FromPersistentPath(_usersFolder, uname));
            _tmpUserName = uname;
        }

        void SaveUser() {
            if (!users.Contains(user.Name))
                users.Add(user.Name);
            user.SaveToPersistentPath(_usersFolder, user.Name);
        }

        void DeleteUser()
        {
            CurrentNode = null;

            DeleteUser_File(user.Name);
            if (users.Count > 0)
                LoadUser(users[0]);
        }
        
        void DeleteUser_File(string uname) {
            QcFile.Delete.FromPersistentFolder(_usersFolder, uname);
            if (users.Contains(uname))
                users.Remove(uname);
        }

        void CreateUser(string uname)
        {
            if (!users.Contains(uname))
            {
                SaveUser();

                user = new CurrentUser
                {
                    Name = uname
                };

                users.Add(uname);
            }
            else Debug.LogError("User {0} already exists".F(uname));
        }

        void RenameUser (string uname) {

            DeleteUser();

            user.Name = uname;

            SaveUser();
        }

        #endregion

        #region Books
        [NonSerialized] public static List<NodeBook_Base> books = new List<NodeBook_Base>();

        public static NodeBook_Base TryGetBook(IBookReference reff) => TryGetBook(reff.BookName, reff.AuthorName);

        public static NodeBook_Base TryGetBook(string bookName, string authorName)
        {
 
            foreach (var b in books)
                if (b.NameForPEGI.Equals(bookName) && b.authorName.Equals(authorName))
                    return b;

            return null;
        }

        public static bool TryGetLoadedBook(IBookReference reff, out NodeBook nodeBook) => TryGetLoadedBook(reff.BookName , reff.AuthorName,  out nodeBook);

        public static bool TryGetLoadedBook(string bookName, string authorName, out NodeBook nodeBook) { 
           

            var book = TryGetBook(bookName, authorName);

            if (book == null) {
                nodeBook = null;
                return false;
            }

            if (book.GetType() == typeof(NodeBook_OffLoaded))
                book = books.LoadBook(book as NodeBook_OffLoaded);

            nodeBook = book as NodeBook;

            return nodeBook != null;
        }

        static readonly string _generalItemsFolder = "General";

        static readonly string _generalItemsFile = "config";

        public static void AddOrReplace(NodeBook nb) {

            var el = books.GetByIGotName(nb);

            if (el != null) {
                if (CurrentNode != null && CurrentNode.parentBook == el)
                    CurrentNode = null;
                books[books.IndexOf(el)] = nb;
            }
            else
                books.Add(nb);
        }

        #endregion

        #region Inspector
        
        private string _tmpUserName;
        
        private int _inspectedBook = -1;
        
        public static bool showPlaytimeUI;
        
        public override void ResetInspector() {
            _inspectReplacementOption = false;
            _tmpUserName = "";
            _replaceReceived = null;
            _inspectedBook = -1;
            base.ResetInspector();

            foreach (var b in books)
                b.ResetInspector();
            
             user.ResetInspector();
        }

        private NodeBook _replaceReceived;
        private bool _inspectReplacementOption;

        public bool InspectAssets()
        {
            var changed = false;

     
            if (changed)
                this.SetToDirty();

            return changed;
        }

        public override bool Inspect() {

            var changed = false;
            
            if (!SaveAnyChanges)
                "Changes will not be saved as user is not a developer".writeWarning();

            if (_inspectedBook == -1) {

                if (inspectedItems == -1) {
                    if (users.Count > 1 && icon.Delete.ClickConfirm("delUsr","Are you sure you want to delete this User?"))
                        DeleteUser();

                    string usr = user.Name;
                    if (pegi.select(ref usr, users)) {
                        SaveUser();
                        LoadUser(usr);
                    }
                }

                if (icon.Enter.enter(ref inspectedItems, 4, "Inspect user"))
                    user.Nested_Inspect().changes(ref changed);

                if (icon.Edit.enter(ref inspectedItems, 6, "Edit or Add user")) {

                    "Name:".edit(60, ref _tmpUserName);

                    if (_tmpUserName.Length <= 3)
                        "Too short".writeHint();
                    else if (users.Contains(_tmpUserName))
                        "Name exists".writeHint();
                    else{
                        if (icon.Add.Click("Add new user"))
                            CreateUser(_tmpUserName);

                        if (icon.Replace.ClickConfirm("rnUsr" ,"Do you want to rename a user {0}? This may break links between his books.".F(user.Name)))
                            RenameUser(_tmpUserName);
                    }
                }

                if (inspectedItems == -1)
                {

                    pegi.nl();

                    if (Application.isEditor) {

                        if (icon.Folder.Click("Open Save files folder").nl())
                            QcFile.Explorer.OpenPersistentFolder(NodeBook_Base.BooksRootFolder);
                        
                        var authorFolders = QcFile.Explorer.GetFolderNamesFromPersistentFolder(NodeBook_Base.BooksRootFolder);

                        foreach (var authorFolder in authorFolders) {

                            var fls = QcFile.Explorer.GetFileNamesFromPersistentFolder(Path.Combine(NodeBook_Base.BooksRootFolder, authorFolder));

                            foreach (var bookFile in fls) {

                                var loaded = TryGetBook(bookFile, authorFolder);

                                if (loaded == null) {
                                    "{0} by {1}".F(bookFile, authorFolder).write();

                                    if (icon.Insert.Click("Add current book to list"))
                                            books.Add(new NodeBook_OffLoaded(bookFile, authorFolder));

                                    pegi.nl();
                                }

                            }
                        }
                    }

                /*
                if (icon.Refresh.Click("Will populate list with mentions with books in Data folder without loading them")) {
                    
                    var lst = QcFile.ExplorerUtils.GetFileNamesFromPersistentFolder(NodeBook_Base.BooksRootFolder);

                    foreach (var e in lst)
                    {
                        var contains = false;
                        foreach (var b in books)
                            if (b.NameForPEGI.SameAs(e)) { contains = true; break; }

                        if (contains) continue;
                        
                        var off = new NodeBook_OffLoaded { name = e };
                        books.Add(off);
                        
                    }
                }*/
                }
            }
            else inspectedItems = -1;

            if (inspectedItems == -1) {

                var newBook = "Books ".edit_List(ref books, ref _inspectedBook, ref changed);

                if (newBook != null)
                    newBook.authorName = user.Name;
                

                if (_inspectedBook == -1)
                {

                    #region Paste Options

                    if (_replaceReceived != null)
                    {

                        if (_replaceReceived.NameForPEGI.enter(ref _inspectReplacementOption))
                            _replaceReceived.Nested_Inspect();
                        else
                        {
                            if (icon.Done.ClickUnFocus()) {

                                var el = books.GetByIGotName(_replaceReceived);
                                if (el != null)
                                    books[books.IndexOf(el)] = _replaceReceived;
                                else books.Add(_replaceReceived);
                                
                                _replaceReceived = null;
                            }
                            if (icon.Close.ClickUnFocus())
                                _replaceReceived = null;
                        }
                    }
                    else  {

                        string tmp = "";
                        if ("Paste Messaged Book".edit(140, ref tmp) || StdExtensions.DropStringObject(out tmp)) {
                            var book = new NodeBook();
                            book.DecodeFromExternal(tmp);
                            if (books.GetByIGotName(book.NameForPEGI) == null)
                                books.Add(book);
                            else
                                _replaceReceived = book;
                        }
                    }
                    pegi.nl();

                    #endregion

                }

            }
            return changed;
            }

        #endregion

        #region Encode_Decode


        public bool Initialize() => this.LoadFromPersistentPath(_generalItemsFolder, _generalItemsFile);

        public void SaveAll()
        {

            SaveUser();

            if (CurrentNode != null)
                CurrentNode.parentBook.UpdatePerBookPresentationConfigs();

            QcFile.Save.ToPersistentPath(_generalItemsFolder, _generalItemsFile, Encode().ToString());
        }


        public override CfgEncoder Encode() => this.EncodeUnrecognized()
            .Add("trigs", TriggerGroup.all)
            .Add("books", books, this)
            .Add_IfTrue("ptUI", showPlaytimeUI)
            .Add("us", users)
            .Add_String("curUser", user.Name);
        
        public override bool Decode(string tg, string data)
        {
            switch (tg)  {
                case "trigs": data.DecodeInto(out TriggerGroup.all); break;
                case "books": data.Decode_List(out books, this); break;
                case "ptUI": showPlaytimeUI = data.ToBool(); break;
                case "us": data.Decode_List(out users); break;
                case "curUser": LoadUser(data); break;
                default: return false;
            }
            return true;
        }

        #endregion
    }
}
