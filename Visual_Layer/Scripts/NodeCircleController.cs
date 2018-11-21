﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SharedTools_Stuff;
using PlayerAndEditorGUI;
using System;
using NodeNotes;

namespace NodeNotes_Visual {

    [ExecuteInEditMode]
    public class NodeCircleController : ComponentSTD, IPEGI, IGotName, IPEGI_ListInspect, IGotIndex, IlinkedLerping {

        static Nodes_PEGI Mgmt => Nodes_PEGI.NodeMGMT_inst;

        public Renderer circleRendy;

        public MeshCollider colly;

        public Base_Node source;

        public string background = "";

        public string backgroundConfig = "";

        bool IsCurrent => source == Shortcuts.CurrentNode;

        Node CurrentNode => Shortcuts.CurrentNode;

        #region Node Image
        Texture coverImage = null;
        public string imageURL = "";
        bool hideLabel = false;
        int imageIndex = -1;
        float imageScaling = 1f;
        enum ImageMode { fit, tile }
        ImageMode mode;

        void LoadCoverImage() {
            if (imageURL.Length > 8)
                imageIndex = Mgmt.textureDownloader.StartDownload(imageURL);
        }

        #endregion

        #region TEXT
        public TextMeshPro textA;

        public TextMeshPro textB;

        bool activeTextIsA = false;

        TextMeshPro ActiveText => activeTextIsA ? textA : textB;

        TextMeshPro PassiveText => activeTextIsA ? textB : textA;

        public string newText = null;

        float activeTextAlpha = 0;

        #endregion

        #region Inspector
        public Base_Node myLastLinkedNode = null;
       
        int indexInPool = 0;
        public int IndexForPEGI { get { return indexInPool;  } set { indexInPool = value; } }


        #if PEGI

        public override bool PEGI_inList(IList list, int ind, ref int edited) {

            bool changed = ActiveConfig.PEGI_inList(list, ind, ref edited);

            if (coverImage)
                this.clickHighlight(coverImage);
            else
                this.clickHighlight();

            return changed;
        }

        public override string NameForPEGI {
            get { return source.name; }

            set
            {
                source.name = value;

                if (fadePortion < 0.1f)
                {
                    newText = null;
                    activeTextAlpha = 1;
                    ActiveText.text = value;
                    gameObject.name = value;
                    PassiveText.text = "";
                    UpdateShaders();
                }
                else
                    newText = value;
            }
        }

        LoopLock loopLock = new LoopLock();

        //int showDependencies = false;
        public override bool Inspect() {

            bool changed = false;
     
            if (loopLock.Unlocked && source != null) {
                using (loopLock.Lock()) {
                    if (source.Try_Nested_Inspect().changes(ref changed)) {
                        if (name != source.name)
                            NameForPEGI = source.name;
                        
                        Shortcuts.visualLayer.UpdateVisibility();
                    }
                }
            } else {
                
                bool onPlayScreen = pegi.paintingPlayAreaGUI;

                if (source != null && source.parentNode == null && icon.Exit.Click("Exit story"))
                    Shortcuts.CurrentNode = null;

                if (source != null)
                {
                    bool enabled = source.Conditions_isEnabled();

                    var nd = source.AsNode;

                    if (nd != null)
                    {
                        if (IsCurrent && nd.parentNode != null && icon.StateMachine.Click("Exit this Node"))
                            Shortcuts.CurrentNode = nd.parentNode;



                        if (!IsCurrent && icon.Enter.Click("Enter this Node"))
                            Shortcuts.CurrentNode = nd;
                    }

                    if ((enabled ? icon.Active : icon.InActive).Click("Try Force Active fconditions to {0}".F(!enabled)) && !source.TryForceEnabledConditions(!enabled))
                        Debug.Log("No Conditions to force to {0}".F(!enabled));

                    if (IsCurrent) {
                        source.name.write(PEGI_Styles.ListLabel);

                        if (source != null && NodesStyleBase.all.selectTypeTag(ref background).nl(ref changed))
                                Nodes_PEGI.SetBackground(this);
                        
                    }
                    else
                        source.name.write("Lerp parameter {0}".F(dominantParameter), enabled ? PEGI_Styles.EnterLabel : PEGI_Styles.ExitLabel);
                }

                    pegi.nl();

                    if (circleRendy)
                    {
                        if (!onPlayScreen) {
                            if (newText != null)
                                "Changeing text to {0}".F(newText).nl();

                            if (isFading)
                                "Fading...{0}".F(fadePortion).nl();
                        }



                        var bg = Mgmt.backgroundControllers.TryGetByTag(background);
                        if (bg != null)
                        {
                            if (bg.Try_Nested_Inspect().nl(ref changed)) {
                                var std = bg as ISTD;
                                if (std != null)
                                    backgroundConfig = std.Encode().ToString();
                            }
                        }
                    }

                    if (source == null || (!source.InspectingTriggerStuff)) {

                        var altVis = PossibleOverrideVisualConfig;
                        var act = ActiveConfig;

                        if (altVis != nodeActive_Default_Visuals) {
                            if ("Override visuals for {0}".F(altVis == nodeInactiveVisuals ? "Disabled" : "Entered")
                                .toggleIcon(ref altVis.enabled, true).nl()) {
                                if (altVis.enabled)
                                    altVis.Decode(act.Encode().ToString());
                            }
                        }
                        changed |= ActiveConfig.Nested_Inspect();
                    }

                    if (!onPlayScreen) {
                        pegi.nl();

                        bool seeDeps = "Dependencies".enter(ref inspectedStuff, 3).nl();

                        if (!textA || seeDeps)
                            "Text A".edit(ref textA).nl();

                        if (!textB || seeDeps)
                            "Text B".edit(ref textB).nl();

                        if (!circleRendy || seeDeps)
                            "Mesh Rendy".edit(ref circleRendy).nl();

                        if (!colly || seeDeps)
                            "Collider".edit(ref colly).nl();
                    }

                    if (inspectedStuff == -1) {

                        if (imageIndex != -1) {
                            if (!pegi.paintingPlayAreaGUI)
                                "Downloading {0} [1]".F(imageURL, imageIndex).write();
                        }  else  {
                            if ("Image".edit("Will not be saved", 40, ref coverImage).nl())
                                SetImage();

                            var shortURL = imageURL;
                            var ind = imageURL.LastIndexOf("/");
                            if (ind > 0)
                                shortURL = imageURL.Substring(ind);

                            bool reload = false;

                            bool changedURL = "Paste URL".edit(90, ref shortURL).changes(ref changed);
                            if (changedURL && (shortURL.Length > 8 || shortURL.Length == 0)) {
                                    reload = true;
                                    imageURL = shortURL;
                            }

                            reload |= (imageURL.Length > 8 && icon.Refresh.Click().changes(ref changed));

                            if (reload)
                                LoadCoverImage();
                            pegi.nl();
                        
                            if (coverImage != null) {

                                if ("Img Mode".editEnum(50, ref mode).nl())
                                    SetImage();

                            if (mode == ImageMode.tile)
                                changed |= "Image Scale".edit(70, ref imageScaling, 1, 10).nl();
                            else
                                changed |= "Hide Label".toggleIcon(ref hideLabel, true).nl();

                                if (!pegi.paintingPlayAreaGUI)
                                    pegi.write(coverImage, 200); pegi.nl();

                            }
                        }
                    }
                
            }
            
            if (changed)
            {
                SetDirty();
                sh_currentColor = ActiveConfig.targetColor;
                UpdateShaders();
            }

            return changed;
        }

        #endif
        #endregion

        #region Visual Configuration
        
        NodeVisualConfig nodeActive_Default_Visuals = new NodeVisualConfig();
        NodeVisualConfig nodeEnteredVisuals = new NodeVisualConfig();
        NodeVisualConfig nodeInactiveVisuals = new NodeVisualConfig();

        NodeVisualConfig PossibleOverrideVisualConfig => 
            (source == null ? nodeActive_Default_Visuals : (source.Conditions_isEnabled()
               ? (IsCurrent ? nodeEnteredVisuals : nodeActive_Default_Visuals)
                : nodeInactiveVisuals));

        NodeVisualConfig ActiveConfig
        {
            get
            {
               var vc = PossibleOverrideVisualConfig;

                if (vc.IsDefault)
                    vc = nodeActive_Default_Visuals;

                return vc;
            }
        }


        #endregion
        
        #region Lerping
        
        Color sh_currentColor;
        Vector4 sh_square = Vector4.zero;
        float sh_blur = 0;

        LinkedLerp.MaterialFloat shadeCourners = new LinkedLerp.MaterialFloat("_Courners", 0, 4);
        LinkedLerp.MaterialFloat shadeSelected = new LinkedLerp.MaterialFloat("_Selected", 0, 4);
        LinkedLerp.MaterialFloat textureFadeIn = new LinkedLerp.MaterialFloat("_TextureFadeIn", 0, 10);
        LinkedLerp.Transform_LocalPosition localPos = new LinkedLerp.Transform_LocalPosition(null, 50);
        LinkedLerp.Transform_LocalScale localScale = new LinkedLerp.Transform_LocalScale(null, 40);
        LinkedLerp.RendererMaterialTextureTransition texTrns = new LinkedLerp.RendererMaterialTextureTransition();

        public void Portion(ref float portion, ref string dominantParameter)  {

            if (!gameObject.activeSelf)
                return;

            if (!lerpsFinished && (this != dragging))  {

                var ac = ActiveConfig;

                if (!isFading || !fadingRelation)
                    localPos.targetValue = ac.targetLocalPosition;
                else {
                    if (!fadingIntoParent)
                        localPos.targetValue = fadingRelation.transform.localPosition
                            + (transform.localPosition - fadingRelation.transform.localPosition).normalized * 50;
                    else
                        localPos.targetValue = fadingRelation.transform.localPosition;
                }
                
                localPos.Portion(ref portion, ref dominantParameter);

                localScale.targetValue = ac.targetSize;

                localScale.Portion(ref portion, ref dominantParameter);

                if (12f.SpeedToMinPortion(ac.targetColor.DistanceRGB(sh_currentColor), ref portion))
                    dominantParameter = "color";

                if (4f.SpeedToMinPortion(fadePortion - (isFading ? 0f : 1f), ref portion))
                    dominantParameter = "fade";

                if (8f.SpeedToMinPortion(1 - activeTextAlpha, ref portion))
                    dominantParameter = "text Alpha";

                textureFadeIn.targetValue = coverImage ? 1 : 0;
                textureFadeIn.Portion(ref portion, ref dominantParameter);

                shadeCourners.targetValue = (this == dragging) ? 0 : (source == Shortcuts.CurrentNode) ? 0.4f : 0.9f;
                shadeCourners.Portion(ref portion, ref dominantParameter);

                shadeSelected.targetValue = (this == Mgmt.selectedNode ? 1f : 0f);
                shadeSelected.Portion(ref portion, ref dominantParameter);

                texTrns.Portion(ref portion, ref dominantParameter);
            }
        }

        public void Lerp(float portion) {

            if (!gameObject.activeSelf)
                return;

            var ac = ActiveConfig;

            bool needShaderUpdate = false;

            if (!lerpsFinished && (this != dragging))  {

                needShaderUpdate = true;

                float teleportPortion = (canJumpToPosition && fadePortion < 0.1f && !isFading) ? 1 : portion;

                sh_blur = Mathf.Lerp(sh_blur, Mathf.Clamp01((transform.localPosition - localPos.targetValue).magnitude * 5), Time.deltaTime * 10);

                sh_currentColor = Color.Lerp(sh_currentColor, ac.targetColor, teleportPortion);
                fadePortion = Mathf.Lerp(fadePortion, isFading ? 0 : 1, portion);

                localPos.Lerp(teleportPortion);
                localScale.Lerp(teleportPortion);
                shadeCourners.Lerp(teleportPortion, circleRendy);
                shadeSelected.Lerp(teleportPortion, circleRendy);
                textureFadeIn.Lerp(teleportPortion, circleRendy);

                texTrns.Lerp(textureFadeIn.value < 0.1f ? 1 : teleportPortion);

                if (portion == 1) {
                    if (!isFading) {
                        ActiveConfig.targetSize = circleRendy.transform.localScale;
                        ActiveConfig.targetLocalPosition = transform.localPosition;
                    }

                    lerpsFinished = true;
                }

                var scale = localScale.Value;

                if (scale.x > 0)
                    sh_square.x = scale.x > scale.y ? ((scale.x - scale.y) / scale.x) : 0;
                else sh_square.x = 0;
                if (scale.y > 0)
                    sh_square.y = scale.y > scale.x ? ((scale.y - scale.x) / scale.y) : 0;
                else sh_square.y = 0;

                var textSize = new Vector2(17 + scale.x * 3, 5f + Mathf.Max(0, (scale.y - 1f) * 3f));

                textA.rectTransform.sizeDelta = textSize;
                textB.rectTransform.sizeDelta = textSize;
            }

            if (newText != null || activeTextAlpha < 1) {
                if (newText == null)
                    activeTextAlpha = Mathf.Lerp(activeTextAlpha, 1, portion);
                else
                    activeTextAlpha = MyMath.Lerp_bySpeed(activeTextAlpha, 1, 4);

                if (activeTextAlpha == 1 && newText != null) {
                    activeTextIsA = !activeTextIsA;
                    ActiveText.text = newText;
                    gameObject.name = newText;
                    activeTextAlpha = 0;
                    newText = null;
                }

                needShaderUpdate = true;
            }

            if (needShaderUpdate)
                UpdateShaders();

            if (fadePortion == 0 && isFading && Application.isPlaying)
                Mgmt.Deactivate(this);
        }
        
        public string dominantParameter;

        void Update() {

            UpdateCoverImage();

            if (this == dragging) {
                if (isFading || !Base_Node.editingNodes || !Input.GetMouseButton(0))
                    dragging = null;
                else  {
                        Vector3 pos;
                        if (upPlane.MouseToPlane(out pos)) {
                            transform.position = pos + dragOffset;
                            ActiveConfig.targetLocalPosition = transform.localPosition;
                        }
                }
                SetDirty();
            }
        }

        #endregion

        #region Controls & Updates

        public void SetStartPositionOn(NodeCircleController previous) {

            canJumpToPosition = false;

            if (previous != null && previous != this)
            {
                if (source.parentNode == CurrentNode)
                {
                    var vis = CurrentNode.visualRepresentation as NodeCircleController;
                    if (vis)
                    transform.localPosition = vis.transform.localPosition;
                    circleRendy.transform.localScale = Vector3.one;
                }
                else
                {
                    transform.localPosition = ActiveConfig.targetLocalPosition + (ActiveConfig.targetLocalPosition - previous.transform.localPosition).normalized * 10f;
                   
                    circleRendy.transform.localScale = ActiveConfig.targetSize * 2f;
                }
            }
            else canJumpToPosition = true;

            SetDirty();
        }
        
        bool canJumpToPosition = false;
        bool fadingIntoParent = false;
        NodeCircleController fadingRelation;

        public void SetFadeAwayRelation(NodeCircleController previous) {
            if (previous != null) {

                if (previous != this) {
                    fadingIntoParent = (CurrentNode != null ? CurrentNode.Contains(previous.myLastLinkedNode) : false)
                       && myLastLinkedNode.parentNode == previous.myLastLinkedNode;  

                    if (fadingIntoParent)
                        fadingRelation = previous;
                    else 
                        fadingRelation = CurrentNode?.visualRepresentation as NodeCircleController;

                } else
                {
                    fadingIntoParent = false;
                    fadingRelation = CurrentNode?.visualRepresentation as NodeCircleController;
                }


            }
            else fadingRelation = null;

            SetDirty();
        }

        [NonSerialized] public bool isFading;
        [NonSerialized] public bool lerpsFinished;

        public bool SetDirty() => lerpsFinished = false;

        float fadePortion = 0;

        void SetImage() {
            var mat = circleRendy.MaterialWhaever();

            texTrns.TargetTexture = coverImage;

            if (mode == ImageMode.fit)
                mat.EnableKeyword("_CLAMP");
            else
                mat.DisableKeyword("_CLAMP");

            SetDirty();
        }

        void UpdateCoverImage() {

            if (imageIndex != -1) {
                if (Mgmt.textureDownloader.TryGetTexture(imageIndex, out coverImage)) {
                    SetImage();
                    imageIndex = -1;
                    SetDirty();
                }
            } 
        }

        void UpdateShaders() {
            if (textB && textA) {

                float textFadePortion = (hideLabel ? (1 - textureFadeIn.value) : 1f) * fadePortion;

                ActiveText.color = new Color(0, 0, 0, activeTextAlpha * textFadePortion);
                PassiveText.color = new Color(0, 0, 0, (1 - activeTextAlpha) * textFadePortion);
            }

            if (circleRendy) {
                sh_currentColor.a = fadePortion;

                var pos = Camera.main.WorldToScreenPoint(transform.position).ToVector2();
                pos.Scale(new Vector2(1f / Screen.width, 1f / Screen.height));

                circleRendy.MaterialWhaever().SetVector("_ProjTexPos", pos.ToVector4(imageScaling));

                circleRendy.MaterialWhaever().SetColor("_Color", sh_currentColor);
                circleRendy.MaterialWhaever().SetVector("_Stretch", sh_square);
                circleRendy.MaterialWhaever().SetFloat("_Blur", sh_blur);

            }
            
        }

        static NodeCircleController dragging = null;
        Vector3 dragOffset = Vector3.zero;
        static Plane upPlane = new Plane(Vector3.up, Vector3.zero);
        public void TryDragAndDrop()
        {
            if (dragging == null && Input.GetMouseButtonDown(0)) {

                if (source.AsGameNode != null)
                    Shortcuts.visualLayer.FromNodeToGame(source.AsGameNode);
                else 
                    Nodes_PEGI.NodeMGMT_inst.SetSelected(this);

                Vector3 pos;
                if (upPlane.MouseToPlane(out pos))  {
                    dragging = this;
                    dragOffset = transform.position - pos;
                }
            }
        }

        public void OnMouseOver() {
            if (!isFading) {
                if (Base_Node.editingNodes)
                    TryDragAndDrop();
                else
                      if (source != null)
                    source.OnMouseOver();
            }
        }

        #endregion
        
        #region Encode & Decode
        public override ISTD Decode(string data)   {
            if (this == dragging)
                dragging = null;

            SetDirty();

            background = "";
            backgroundConfig = "";
            imageURL = "";
            imageIndex -= 1;
            coverImage = null;
            hideLabel = false;

            nodeEnteredVisuals = new NodeVisualConfig();
            nodeActive_Default_Visuals = new NodeVisualConfig();
            nodeActive_Default_Visuals.enabled = true;
            nodeInactiveVisuals = new NodeVisualConfig();

            base.Decode(data);

            LoadCoverImage();

            return this;
        }

        public override bool Decode(string tag, string data)   {
            switch (tag)   {
                case "expVis": data.DecodeInto(out nodeEnteredVisuals); break;
                case "subVis": data.DecodeInto(out nodeActive_Default_Visuals); break;
                case "disVis": data.DecodeInto(out nodeInactiveVisuals); break;
                case "bg": background = data; break;
                case "bg_cfg": backgroundConfig = data; break;
                case "URL": imageURL = data; break;
                case "imgScl": imageScaling = data.ToFloat(); break;
                case "imgMd": mode = (ImageMode)data.ToInt(); break;
                case "hidTxt": hideLabel = data.ToBool(); break; 
                default: return false;
            }

            return true;
        }

        public override StdEncoder Encode() {

            var cody = this.EncodeUnrecognized()
                .Add("subVis", nodeActive_Default_Visuals)
                .Add_IfNotDefault("disVis", nodeInactiveVisuals)
                .Add_IfNotEmpty("bg", background)
                .Add_IfNotEmpty("bg_cfg", backgroundConfig)
                .Add_IfNotEmpty("URL", imageURL);

            if (imageURL.Length > 0) {
                cody.Add("imgMd", (int)mode);
                if (mode == ImageMode.tile) 
                    cody.Add("imgScl", imageScaling);
                        else
                    cody.Add_IfTrue("hidTxt", hideLabel);
            }

            if (source.AsNode != null)
                cody.Add_IfNotDefault("expVis", nodeEnteredVisuals); 

            return cody;
        }

        #endregion

        #region MGMT
        public void LinkTo(Base_Node node) {

            myLastLinkedNode = node;
            source = node;
            if (source.visualRepresentation != null)
                Debug.LogError("Visual representation is not null",this);
            source.visualRepresentation = this;
            source.previousVisualRepresentation = this;
            Decode(source.configForVisualRepresentation);
            NameForPEGI = source.name;
            isFading = false;
            gameObject.SetActive(true);
            if (colly)
                colly.enabled = true;
        }

        public void Unlink()
        {
            if (source != null) {
                source.configForVisualRepresentation = Encode().ToString();
                source.visualRepresentation = null;
                source = null;
            }
       
                isFading = true;

            if (colly)
                colly.enabled = false;
        }

        private void OnEnable() {

            localPos._transform = transform;
            localScale._transform = circleRendy.transform;
            texTrns.Renderer = circleRendy;

            if (Application.isEditor) {

                if (!circleRendy)
                    circleRendy = GetComponent<MeshRenderer>();
            }
        }

        void OnDisable() {
            if (!isFading)
                Unlink();
        }


        #endregion
    }

    public class NodeVisualConfig : AbstractKeepUnrecognized_STD, IPEGI, IPEGI_ListInspect, ICanBeDefault_STD {
        public Vector3 targetSize = new Vector3(5,3,1);
        public Vector3 targetLocalPosition = Vector3.zero;
        public Color targetColor = Color.gray;
        public bool enabled = false;

        #region Encode & Decode
        public override bool IsDefault => !enabled;

        public override ISTD Decode(string data) {
            enabled = true;
            return base.Decode(data);
        }

        public override bool Decode(string tag, string data)
        {
            switch (tag)  {
                case "sc": targetSize = data.ToVector3(); break;
                case "pos": targetLocalPosition = data.ToVector3(); break;
                case "col": targetColor = data.ToColor(); break;
                default: return false;
            }

            return true;
        }

        public override StdEncoder Encode()  {
            targetSize.z = Mathf.Max(targetSize.z, 1);

            var cody = this.EncodeUnrecognized()
                .Add("sc", targetSize)
                .Add("pos", targetLocalPosition);
            if (targetColor != Color.grey)
                cody.Add("col", targetColor);

            return cody;
        }
        #endregion

        #region Inspect
        #if PEGI

        public override bool Inspect() {

            bool changed = false;

            float x = targetSize.x;
            if ("Width".edit(50, ref x, 1f, 15f).nl(ref changed))  
                targetSize.x = x;
            
            float y = targetSize.y;
            if ("Height".edit(50, ref y, 1f, 15f).nl(ref changed)) 
                targetSize.y = y;
            
            "Color".edit(50, ref targetColor).nl(ref changed);
            
            return changed;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited) {

            var changed = "col".edit(40, ref targetColor);

            if (icon.Enter.Click())
                edited = ind;

            return changed;
        }
#endif
        #endregion
    }

}