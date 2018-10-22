﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedTools_Stuff;
using PlayerAndEditorGUI;
using STD_Logic;
using System.Collections;

namespace NodeNotes {

    public class GameNodeAttribute : Abstract_WithTaggedTypes {
        public override TaggedTypes_STD TaggedTypes => GameNodeBase.all;
    }

    [GameNode]
    [DerrivedList()]
    public abstract class GameNodeBase : Base_Node, IGotClassTag, IPEGI_ListInspect {
        #region Tagged Types MGMT
        public override GameNodeBase AsGameNode => this;
        public virtual string ClassTag => StdEncoder.nullTag;
        public static TaggedTypes_STD all = new TaggedTypes_STD(typeof(GameNodeBase));
        public TaggedTypes_STD AllTypes => all;
        #endregion

        #region Enter & Exit
        protected virtual void AfterEnter() { }

        protected virtual void ExitInternal() { }

        protected LoopLock loopLock = new LoopLock();

        public void Enter() {
            if (loopLock.Unlocked)
                using (loopLock.Lock()) {

                    var data = Shortcuts.user.gameNodeTypeData.TryGet(ClassTag);
                    if (data != null) Decode_PerUserData(data);
                    data = parentNode.root.gameNodeTypeData.TryGet(ClassTag);
                    if (data != null) Decode_PerBookData(data);

                    VisualLayer.FromNodeToGame(this);

                    results.Apply();

                    AfterEnter();
                }
        }

        public void FailExit() {
            if (loopLock.Unlocked)
                using (loopLock.Lock()) {
                    ExitInternal();
                    VisualLayer.FromGameToNode(true);
                }
        }

        public void Exit() {
            if (loopLock.Unlocked)
                using (loopLock.Lock()) {

                    ExitInternal();
                    VisualLayer.FromGameToNode(false);

                    Shortcuts.user.gameNodeTypeData[ClassTag] = Encode_PerUserData().ToString();
                    parentNode.root.gameNodeTypeData[ClassTag] = Encode_PerBookData().ToString();

                    onExitResults.Apply();
                }
        }
        #endregion

        #region Inspector
        protected virtual bool InspectGameNode() => false;

        protected override string ResultsRole => "On Enter";

        protected virtual string ExitResultRole => "On Exit";

        int editedExitResult = -1;

        public override bool Inspect()
        {
            bool changed = false;
        
            changed |= base.Inspect();

            changed |= ExitResultRole.enter_List(onExitResults, ref editedExitResult, ref inspectedStuff, 7).nl_ifFalse();

            if (ClassTag.enter(ref inspectedStuff, 6).nl_ifFalse())
                InspectGameNode();

            return changed;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited) {

            bool changed = this.inspect_Name();

            if (icon.Enter.Click())
                edited = ind;

            if (VisualLayer.IsCurrentGameNode(this)) {
                if (icon.Close.Click("Exit Game Node in Fail"))
                    VisualLayer.FromGameToNode(true);

                if (icon.Exit.Click("Exit Game Node"))
                    VisualLayer.FromGameToNode();

            } else
            {
                if (icon.Play.Click("Enter Game Node"))
                    VisualLayer.FromNodeToGame(this);
            }

            return changed;
        }

        #endregion

        #region Encode & Decode
        public List<Result> onExitResults = new List<Result>();

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("b", base.Encode)
            .Add("exit", onExitResults);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "b": data.DecodeInto(base.Decode); break;
                case "exit": data.DecodeInto_List(out onExitResults); break;
                default: return false;
            }
            return true;
        }

        // Per User data will be Encoded/Decoded each time the node is Entered during play
        public virtual StdEncoder Encode_PerUserData() => new StdEncoder();
        public virtual bool Decode_PerUser(string tag, string data) => true;
        public virtual void Decode_PerUserData(string data) => data.DecodeInto(Decode_PerUser);

        // Per Node Book Data: Data will be encoded each time Node Book is Saved
        public virtual StdEncoder Encode_PerBookData() => new StdEncoder();
        public virtual bool Decode_PerBook(string tag, string data) => true;
        public virtual void Decode_PerBookData(string data) => data.DecodeInto(Decode_PerBook);
        #endregion
    }

    [TaggedType(classTag)]
    public class GameNodeTest0 : GameNodeBase {
        const string classTag = "test0";

        public override string ClassTag => classTag;

    }

    [TaggedType(classTag, "Test Node 1")]
    public class GameNodeTest1 : GameNodeBase
    {
        const string classTag = "testSame";

        public override string ClassTag => classTag;

    }

    [TaggedType(classTag, "test2")]
    public class GameNodeTest2 : GameNodeBase
    {
        const string classTag = "test2";

        public override string ClassTag => classTag;

    }

}
