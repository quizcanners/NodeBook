﻿using System;
using NodeNotes_Visual;
using PlaytimePainter;
using PlaytimePainter.UI;
using QuizCannersUtilities;
using TMPro;
using UnityEngine;

[ExecuteAlways] 
public class DialogueUI_SpeechBox : MonoBehaviour, ILinkedLerping, IManageFading {

    private DialogueUI Mgmt => DialogueUI.instance;

    private NodesVisualLayer VisualMgmt => NodesVisualLayer.Instance;

    public TextMeshProUGUI text;

    public RoundedGraphic graphic;

    public RectTransform rectTransform;

    public string Text { set { text.text = value; } }

    [NonSerialized] public int index;

    public bool isHistory;

    public void Click() => DialogueUI.instance.Click(index);
    

    [NonSerialized] public bool isFirst = false;

    [NonSerialized] public bool isLast = false;
    
    public bool deformingFinished;

    public void Update() {

        if (!deformingFinished) {

            float upper = graphic.GetCorner(true, false);
            float lover = graphic.GetCorner(false, false);

        }

    }

    LinkedLerp.FloatValue upperEdge = new LinkedLerp.FloatValue( 0, 8, "Upper");
    LinkedLerp.FloatValue loverEdge = new LinkedLerp.FloatValue( 0 , 8, "Lower");
    LinkedLerp.FloatValue transparency = new LinkedLerp.FloatValue( 0, 8, "Alpha");

    public void Portion(LerpData ld) {

        transparency.Portion(ld, isFadingOut ? 0 : 1);
        upperEdge.Portion(ld, isFirst ? 0 : 1);
        loverEdge.Portion(ld, isLast ? 0 : 1);

    }

    public void Lerp(LerpData ld, bool canSkipLerp) {

        transparency.Lerp(ld);
        upperEdge.Lerp(ld);
        loverEdge.Lerp(ld);

        graphic.SetCorner(RoundedGraphic.Corner.Up_Left, upperEdge.CurrentValue);
        graphic.SetCorner(RoundedGraphic.Corner.Up_Right, upperEdge.CurrentValue);

        graphic.SetCorner(RoundedGraphic.Corner.Down_Left, loverEdge.CurrentValue);
        graphic.SetCorner(RoundedGraphic.Corner.Down_Right, loverEdge.CurrentValue);

        graphic.TrySetAlpha(transparency.CurrentValue);
        text.TrySetAlpha(transparency.CurrentValue);

        if (isFadingOut && ld.Portion() == 1f) {
            if (isHistory)
                Mgmt.historyPool.Disable(this);
            else 
                Mgmt.optionsPool.Disable(this);
        }


    }

    public bool isFadingOut;

    public void FadeAway() => isFadingOut = true;

    public bool TryFadeIn() {
        isFadingOut = false;
        graphic.TrySetAlpha(0);
        return true;
    }
}
