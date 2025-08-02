using UnityEngine;
using UnityEngine.UI;

public class BuneLean : MonoBehaviour
{
    [SerializeField]
    public GameObject buttons, sb, replayButton, levelSuccess, lb, rb, tw, nslo, ib, bb, bbbb, co, taptaptaptap, sbad;
    public GameObject[] markethm, lbns;
    public Text[] text;
    public Image bg, ak;
    public bool ttbl = false, hm = false, ae = false;
    public UIManager ui;
    public TW tywr;
    public GameManager gm;
    public void Ad()
    {
        LeanTween.scale(ui.ds, new Vector3(0.8f, 0.8f, 1f), 0.4f).setEase(LeanTweenType.easeInOutCubic).setOnComplete(ad2);
    }

    public void ad2()
    {
        LeanTween.scale(ui.ds, new Vector3(1f, 1f, 1f), 0.6f).setEase(LeanTweenType.easeInOutCubic).setOnComplete(Ad);
    }

    public void Button(GameObject target, GameObject buton)
    {
        Transform[] children = target.GetComponentsInChildren<Transform>();

        target.SetActive(true);
        
        if (buton != null)
        {
            LeanTween.scale(buton.gameObject, new Vector3(0.96f, 0.96f, 1f), 0.1f).setEase(LeanTweenType.easeInOutCubic).setOnComplete(() => { LeanTween.scale(buton.gameObject, new Vector3(1f, 1f, 1f), 0.1f).setEase(LeanTweenType.easeInOutCubic); });
        }

        foreach (Transform child in children)
        {
            if (child.name == "Items")
            {
                LeanTween.scale(child.gameObject, new Vector3(1.02f, 1.02f, 1f), 0.1f).setEase(LeanTweenType.easeInOutCubic).setOnComplete(() => { LeanTween.scale(child.gameObject, new Vector3(1f, 1f, 1f), 0.1f).setEase(LeanTweenType.easeInOutCubic); });
            }
        }
    }
    
    public void cool(GameObject hm)
    {
        LeanTween.scale(hm, new Vector3(0f, 0f, 1f), 0.4f).setEase(LeanTweenType.easeInOutCubic).setOnComplete(() =>
        {
            hm.SetActive(false);
            LeanTween.scale(hm, new Vector3(1f, 1f, 1f), 0f).setEase(LeanTweenType.easeInOutCubic);
        });
    }

    public void open(GameObject hm)
    {
        LeanTween.scale(hm, new Vector3(0f, 0f, 1f), 0f).setEase(LeanTweenType.easeInOutCubic).setOnComplete(() =>
        {
            hm.SetActive(true);
            LeanTween.scale(hm, new Vector3(1f, 1f, 1f), 0.4f).setEase(LeanTweenType.easeInOutCubic);
        });
    }

    public void StartTT()
    {
        ui.taptap.SetActive(false);
        sbad.SetActive(false);
        ui.ab.GetComponent<Button>().interactable = false;
        rb.GetComponent<Button>().interactable = false;
        lb.GetComponent<Button>().interactable = false;
        ui.ShopB.GetComponent<Button>().interactable = false;
        ui.htpBtn.GetComponent<Button>().interactable = false;
        ui.lb.GetComponent<Button>().interactable = false;
        ui.newsBtn.GetComponent<Button>().interactable = false;
        ui.menuButtons.GetComponent<Button>().interactable = false;
        ui.code.GetComponent<Button>().interactable = false;
        ui.dBtn.GetComponent<Button>().interactable = false;
        ui.tapToStart.GetComponent<Button>().interactable = false;
        ui.kb.GetComponent<Button>().interactable = false;
        LeanTween.value(ui.BgUI.gameObject, ui.BgUI.GetComponent<Image>().color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color objColor = ui.BgUI.GetComponent<Image>().color; objColor.a = alpha; ui.BgUI.GetComponent<Image>().color = objColor; }).setTime(0.4f);
        LeanTween.scale(ui.BgUI, new Vector3(0f, 0f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.scale(nslo, new Vector3(0f, 0f, 1f), 0f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.scale(ui.abui, new Vector3(0f, 0f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.scale(ui.tapToStart, new Vector3(0f, 0f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.delayedCall(0.5f, () =>
        {
            LeanTween.scale(ui.chrctr, new Vector3(0f, 0f, 1f), 1f).setEase(LeanTweenType.easeOutCubic);
        });

        LeanTween.delayedCall(2f, () =>
        {
            ui.Tt.SetActive(true);
            LeanTween.scale(levelSuccess, new Vector3(1.2f, 1.2f, 1.2f), 2f).setEase(LeanTweenType.easeOutElastic);
            LeanTween.moveLocal(tw, new Vector3(-0f, -5f, 0f), 1f).setEase(LeanTweenType.easeOutCirc);
            LeanTween.moveLocal(rb, new Vector3(350f, 389.4359f, 0f), 1f).setEase(LeanTweenType.easeOutCirc);
            LeanTween.moveLocal(lb, new Vector3(-350f, 389.4359f, 0f), 0.7f).setEase(LeanTweenType.easeOutCirc);
            LeanTween.moveLocal(levelSuccess, new Vector3(6f, -40f, 2f), .7f).setDelay(2f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.scale(levelSuccess, new Vector3(1.5f, 1.5f, 1.5f), 2f).setDelay(1.7f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.scale(ui.title, new Vector3(0f, 0f, 1.5f), 1.6f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.scale(nslo, new Vector3(1f, 1f, 1f), 2f).setDelay(1.5f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.scale(ui.newsBtn, new Vector3(0f, 0f, 1f), 1.8f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.scale(ui.kb, new Vector3(0f, 0f, 1f), 1.8f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.scale(ui.vertex, new Vector3(0f, 0f, 1f), 1.8f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.scale(ui.menuButtons, new Vector3(0f, 0f, 1f), 1.8f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.scale(ui.code, new Vector3(0f, 0f, 1f), 1.8f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.scale(ui.ab, new Vector3(0f, 0f, 1.5f), 1.9f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.value(bg.gameObject, bg.GetComponent<Image>().color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color objColor = bg.GetComponent<Image>().color; objColor.a = alpha; bg.GetComponent<Image>().color = objColor; });
            LeanTween.moveLocal(buttons, new Vector3(-15.02336f, -500f, 0f), 1f).setDelay(1f).setEase(LeanTweenType.easeOutCirc);
            LeanTween.moveLocal(sb, new Vector3(4.2f, -180f, 1f), 2f).setDelay(3f).setEase(LeanTweenType.easeOutCirc);
        });

        LeanTween.delayedCall(7f, () =>
        {
            tywr.StartTypewriter(0);
        });

        LeanTween.delayedCall(14.4f, () =>
        {
            tywr.timeBtwChars = 0.002f;
            tywr.StartTypewriter(1);
            ui.taptap.SetActive(true);
        });
    }

    public void Phase2()
    {
        tywr.timeBtwChars = 0.048f;
        LeanTween.value(tywr.texts[0].gameObject, tywr.texts[0].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[0].color; textColor.a = alpha; tywr.texts[0].color = textColor;});
        LeanTween.value(tywr.texts[1].gameObject, tywr.texts[1].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[1].color; textColor.a = alpha; tywr.texts[1].color = textColor;});
        LeanTween.scale(ui.lbui, new Vector3(0.001f, 0.001f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.value(ui.lbui.gameObject, ui.lbui.GetComponent<Image>().color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color objColor = ui.lbui.GetComponent<Image>().color; objColor.a = alpha; ui.lbui.GetComponent<Image>().color = objColor; });
        LeanTween.value(ui.taptap.gameObject, ui.taptap.GetComponentInChildren<Text>().color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = ui.taptap.GetComponentInChildren<Text>().color; textColor.a = alpha; ui.taptap.GetComponentInChildren<Text>().color = textColor;});
        ui.taptap.GetComponent<Button>().interactable = false;
        LeanTween.delayedCall(1f, () =>
        {
            tywr.StartTypewriter(2);
            ui.taptap.SetActive(false);
        });
        LeanTween.scale(ui.ab, new Vector3(0.5f, 0.5f, 1f), 2f).setEase(LeanTweenType.easeOutElastic);
        LeanTween.moveLocal(ui.ab, new Vector3(-8f, 260f, 2f), .7f).setDelay(1f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.scale(ui.ab, new Vector3(1.5f, 1.5f, 1.5f), 2f).setDelay(1.5f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.delayedCall(7.6f, () =>
        {
            ui.ab.GetComponent<Button>().interactable = true;
            ttbl = true;
        });
    }

    public void Phase3()
    {
        foreach (GameObject obj in lbns)
        {
            obj.SetActive(false);
        }
        ui.ab.GetComponent<Button>().interactable = false;
        LeanTween.moveLocal(ui.lbui, new Vector3(12f, 50f, 1f), 0.5f).setEase(LeanTweenType.easeInOutCubic);
        ib.GetComponent<Button>().interactable = false;
        LeanTween.value(ui.abui.gameObject, ui.abui.GetComponent<Image>().color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color objColor = ui.abui.GetComponent<Image>().color; objColor.a = alpha; ui.abui.GetComponent<Image>().color = objColor; });
        LeanTween.scale(ui.abui, new Vector3(0f, 0f, 1f), 0f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.scale(ui.ab, new Vector3(0f, 0f, 1f), 0.5f).setEase(LeanTweenType.easeInOutCubic);
        ui.abui.SetActive(true);
        LeanTween.scale(ui.abui, new Vector3(1f, 1f, 1f), 1f).setDelay(2f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.value(tywr.texts[2].gameObject, tywr.texts[2].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[2].color; textColor.a = alpha; tywr.texts[2].color = textColor; }).setTime(0.4f);
        LeanTween.moveLocal(levelSuccess, new Vector3(6f, -300f, 1f), 0.5f).setDelay(1.5f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.moveLocal(sb, new Vector3(4.2f, -450f, 1f), 0.5f).setDelay(1f).setEase(LeanTweenType.easeOutCirc);
        LeanTween.delayedCall(2.5f, () =>
        {
            tywr.StartTypewriter(3);
        });
        LeanTween.delayedCall(8.4f, () =>
        {
            tywr.timeBtwChars = 0.004f;
            tywr.StartTypewriter(4);
        });
        LeanTween.delayedCall(8.8f, () =>
        {
            ib.GetComponent<Button>().interactable = true;
            tywr.timeBtwChars = 0.048f;
        });
    }

    public void Phase4()
    {
        LeanTween.moveLocal(ui.ab, new Vector3(205.5f, 670.6199f, 1f), .7f).setDelay(1f).setEase(LeanTweenType.easeInOutCubic);
        hm = true;
        ui.blglndrm.SetActive(false);
        ui.lbui.SetActive(true);
        bb.GetComponent<Button>().interactable = false;
        bbbb.GetComponent<Button>().interactable = false;
        LeanTween.moveLocal(taptaptaptap, new Vector3(-7.817652f, -560f, 1f), 0.5f).setDelay(1f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.scale(ui.lbui, new Vector3(0.8f, 0.8f, 1f), 1f).setDelay(2f).setEase(LeanTweenType.easeOutCubic);
        LeanTween.moveLocal(levelSuccess, new Vector3(6f, -200f, 1f), 0.5f).setDelay(0.5f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.moveLocal(sb, new Vector3(4.2f, -330f, 1f), 0.5f).setDelay(1f).setEase(LeanTweenType.easeOutCirc);
        LeanTween.scale(ui.abui, new Vector3(0f, 0f, 1f), 0.5f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.value(tywr.texts[3].gameObject, tywr.texts[3].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[3].color; textColor.a = alpha; tywr.texts[3].color = textColor; }).setTime(0.4f);
        LeanTween.value(tywr.texts[4].gameObject, tywr.texts[4].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[4].color; textColor.a = alpha; tywr.texts[4].color = textColor; }).setTime(0.4f);
        LeanTween.delayedCall(2f, () =>
        {
            tywr.timeBtwChars = 0.032f;
            tywr.StartTypewriter(5);
        });
        LeanTween.delayedCall(12f, () =>
        {
            ui.taptap.GetComponent<Button>().interactable = true;
            ui.taptap.SetActive(true);
            LeanTween.value(ui.taptap.gameObject, ui.taptap.GetComponentInChildren<Text>().color.a, 1f, 0f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = ui.taptap.GetComponentInChildren<Text>().color; textColor.a = alpha; ui.taptap.GetComponentInChildren<Text>().color = textColor; });
        });
    }

    public void Phase5()
    {
        ae = true;
        LeanTween.scale(ui.lbui, new Vector3(0f, 0f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.moveLocal(levelSuccess, new Vector3(6f, 40f, 1f), 0.5f).setDelay(1f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.moveLocal(sb, new Vector3(4.2f, -100f, 1f), 0.5f).setDelay(1.5f).setEase(LeanTweenType.easeOutCirc);
        LeanTween.value(tywr.texts[5].gameObject, tywr.texts[5].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[5].color; textColor.a = alpha; tywr.texts[5].color = textColor; }).setTime(0.4f);
        LeanTween.value(ui.taptap.gameObject, ui.taptap.GetComponentInChildren<Text>().color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = ui.taptap.GetComponentInChildren<Text>().color; textColor.a = alpha; ui.taptap.GetComponentInChildren<Text>().color = textColor; });
        ui.taptap.GetComponent<Button>().interactable = false;
        LeanTween.delayedCall(2f, () =>
        {
            ui.taptap.SetActive(false);
            tywr.StartTypewriter(6);
        });
        LeanTween.delayedCall(22f, () =>
        {
            ui.taptap.GetComponent<Button>().interactable = true;
            ui.taptap.SetActive(true);
            LeanTween.value(ui.taptap.gameObject, ui.taptap.GetComponentInChildren<Text>().color.a, 1f, 0f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = ui.taptap.GetComponentInChildren<Text>().color; textColor.a = alpha; ui.taptap.GetComponentInChildren<Text>().color = textColor; });
        });
    }

    public void Phase6()
    {
        tywr.timeBtwChars = 0.04f;
        LeanTween.value(ui.taptap.gameObject, ui.taptap.GetComponentInChildren<Text>().color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = ui.taptap.GetComponentInChildren<Text>().color; textColor.a = alpha; ui.taptap.GetComponentInChildren<Text>().color = textColor; });
        ui.taptap.GetComponent<Button>().interactable = false;
        LeanTween.value(tywr.texts[6].gameObject, tywr.texts[6].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[6].color; textColor.a = alpha; tywr.texts[6].color = textColor; }).setTime(0.4f);
        LeanTween.moveLocal(levelSuccess, new Vector3(6f, -350f, 1f), 0.5f).setDelay(1.5f).setEase(LeanTweenType.easeInOutCubic);
        LeanTween.moveLocal(sb, new Vector3(4.2f, -500f, 1f), 0.5f).setDelay(1f).setEase(LeanTweenType.easeOutCirc);

        LeanTween.delayedCall(2f, () =>
        {
            ui.taptap.SetActive(false);
            tywr.StartTypewriter(7);
        });

        LeanTween.delayedCall(8f, () =>
        {
            LeanTween.moveLocal(levelSuccess, new Vector3(6f, -240f, 1f), 0.5f).setDelay(1f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.moveLocal(sb, new Vector3(4.2f, -390f, 1f), 0.5f).setDelay(1.5f).setEase(LeanTweenType.easeOutCirc);
            LeanTween.value(tywr.texts[7].gameObject, tywr.texts[7].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[7].color; textColor.a = alpha; tywr.texts[7].color = textColor; }).setTime(0.4f);
        });

        LeanTween.delayedCall(10f, () =>
        {
            LeanTween.scale(markethm[0], new Vector3(1f, 1f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.value(text[0].gameObject, text[0].color.a, 1f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) =>{Color textColor = text[0].color;textColor.a = alpha;text[0].color = textColor;});
            tywr.StartTypewriter(8);
        });

        LeanTween.delayedCall(19f, () =>
        {
            LeanTween.scale(markethm[0], new Vector3(0f, 0f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.value(text[0].gameObject, text[0].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = text[0].color; textColor.a = alpha; text[0].color = textColor; });
            LeanTween.value(tywr.texts[8].gameObject, tywr.texts[8].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[8].color; textColor.a = alpha; tywr.texts[8].color = textColor; });
        });

        LeanTween.delayedCall(20f, () =>
        {
            LeanTween.scale(markethm[1], new Vector3(1f, 1f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.value(text[1].gameObject, text[1].color.a, 1f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = text[1].color; textColor.a = alpha; text[1].color = textColor; });
            tywr.StartTypewriter(9);
        });

        LeanTween.delayedCall(27f, () =>
        {
            LeanTween.scale(markethm[1], new Vector3(0f, 0f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.value(text[1].gameObject, text[1].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = text[1].color; textColor.a = alpha; text[1].color = textColor; });
            LeanTween.value(tywr.texts[9].gameObject, tywr.texts[9].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[9].color; textColor.a = alpha; tywr.texts[9].color = textColor; });
        });

        LeanTween.delayedCall(28f, () =>
        {
            LeanTween.scale(markethm[2], new Vector3(1f, 1f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.value(text[2].gameObject, text[2].color.a, 1f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = text[2].color; textColor.a = alpha; text[2].color = textColor; });
            tywr.StartTypewriter(10);
        });

        LeanTween.delayedCall(34f, () =>
        {
            LeanTween.scale(markethm[2], new Vector3(0f, 0f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.value(text[2].gameObject, text[2].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = text[2].color; textColor.a = alpha; text[2].color = textColor; });
            LeanTween.value(tywr.texts[10].gameObject, tywr.texts[10].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[10].color; textColor.a = alpha; tywr.texts[10].color = textColor; });
        });

        LeanTween.delayedCall(35f, () =>
        {
            LeanTween.scale(markethm[3], new Vector3(1f, 1f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.value(text[3].gameObject, text[3].color.a, 1f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = text[3].color; textColor.a = alpha; text[3].color = textColor; });
            tywr.StartTypewriter(11);
        });

        LeanTween.delayedCall(49f, () =>
        {
            LeanTween.scale(markethm[3], new Vector3(0f, 0f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.value(text[3].gameObject, text[3].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = text[3].color; textColor.a = alpha; text[3].color = textColor; });
            LeanTween.value(tywr.texts[11].gameObject, tywr.texts[11].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[11].color; textColor.a = alpha; tywr.texts[11].color = textColor; });
        });

        LeanTween.delayedCall(50f, () =>
        {
            LeanTween.scale(markethm[4], new Vector3(1f, 1f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.value(text[4].gameObject, text[4].color.a, 1f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = text[4].color; textColor.a = alpha; text[4].color = textColor; });
            tywr.StartTypewriter(12);
        });

        LeanTween.delayedCall(62f, () =>
        {
            LeanTween.scale(markethm[4], new Vector3(0f, 0f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.value(text[4].gameObject, text[4].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = text[4].color; textColor.a = alpha; text[4].color = textColor; });
            LeanTween.value(tywr.texts[12].gameObject, tywr.texts[12].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[12].color; textColor.a = alpha; tywr.texts[12].color = textColor; });
        });

        LeanTween.delayedCall(63f, () =>
        {
            LeanTween.scale(markethm[5], new Vector3(1f, 1f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.value(text[5].gameObject, text[5].color.a, 1f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = text[5].color; textColor.a = alpha; text[5].color = textColor; });
            tywr.StartTypewriter(13);
        });

        LeanTween.delayedCall(67f, () =>
        {
            LeanTween.scale(markethm[5], new Vector3(0f, 0f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.value(text[5].gameObject, text[5].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = text[5].color; textColor.a = alpha; text[5].color = textColor; });
            LeanTween.value(tywr.texts[13].gameObject, tywr.texts[13].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[13].color; textColor.a = alpha; tywr.texts[13].color = textColor; });
        });

        LeanTween.delayedCall(68f, () =>
        {
            Phase7();
        });
    }

    public void Phase7()
    {
        tywr.StartTypewriter(14);
        LeanTween.delayedCall(6f, () =>
        {
            ui.ab.GetComponent<Button>().interactable = false;
            ui.ab.SetActive(true);
            LeanTween.value(tywr.texts[14].gameObject, tywr.texts[14].color.a, 0f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = tywr.texts[14].color; textColor.a = alpha; tywr.texts[14].color = textColor; });
        });
        LeanTween.delayedCall(7f, () =>
        {
            LeanTween.moveLocal(sb, new Vector3(4.2f, -700f, 1f), 1f).setEase(LeanTweenType.easeOutCirc);
            LeanTween.scale(levelSuccess, new Vector3(0f, 0f, 1f), 1f).setEase(LeanTweenType.easeOutCubic);
        });
        LeanTween.delayedCall(8f, () =>
        {
            ui.ab.GetComponent<Button>().interactable = false;
            ui.ab.SetActive(true);
            LeanTween.value(bg.gameObject, bg.GetComponent<Image>().color.a, 1f, 1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color objColor = bg.GetComponent<Image>().color; objColor.a = alpha; bg.GetComponent<Image>().color = objColor; }).setTime(2f);
            LeanTween.moveLocal(rb, new Vector3(185.4732f, 388.502f, 0f), 1f).setEase(LeanTweenType.easeOutCirc);
            LeanTween.moveLocal(lb, new Vector3(-215.0962f, 389.002f, 0f), 0.7f).setEase(LeanTweenType.easeOutCirc);
            LeanTween.scale(ui.title, new Vector3(1f, 1f, 1.5f), 0.5f).setDelay(1.5f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.scale(nslo, new Vector3(0f, 0f, 1f), 1.5f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.scale(ui.newsBtn, new Vector3(1f, 1f, 1f), 1.8f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.scale(ui.menuButtons, new Vector3(1f, 1f, 1f), 1.8f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.scale(ui.code, new Vector3(1f, 1f, 1f), 1.8f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.scale(ui.ab, new Vector3(1f, 1f, 1f), 1.9f).setEase(LeanTweenType.easeInOutCubic);
            LeanTween.moveLocal(tw, new Vector3(-0f, 3.050003f, 0f), 1f).setEase(LeanTweenType.easeOutCirc);
            LeanTween.scale(ui.chrctr, new Vector3(0.130732f, 0.15f, 1f), 1f).setEase(LeanTweenType.easeOutCubic);
            LeanTween.moveLocal(buttons, new Vector3(-15.02336f, -363.9228f, 0f), 1f).setDelay(1f).setEase(LeanTweenType.easeOutCirc);
            LeanTween.scale(ui.tapToStart, new Vector3(1f, 1f, 1f), 1f).setEase(LeanTweenType.easeInOutCubic);
        });
        LeanTween.delayedCall(10f, () =>
        {
            ui.BGFN();
        });
    }

    public void AdInfo()
    {
        LeanTween.scale(levelSuccess, new Vector3(0f, 0f, 0f), 0f).setEase(LeanTweenType.easeOutCirc);
        LeanTween.scale(sbad, new Vector3(0f, 0f, 0f), 0f).setEase(LeanTweenType.easeOutCirc);
        LeanTween.moveLocal(levelSuccess, new Vector3(4.7684e-07f, 383.38f, 1f), 0f).setEase(LeanTweenType.easeInOutCubic);
        nslo.SetActive(false);
        ui.Tt.SetActive(true);
        ui.restartBtn.GetComponent<Button>().interactable = false;
        ui.htpBtn.GetComponent<Button>().interactable = false;
        ui.discordsw.GetComponent<Button>().interactable = false;
        ui.ds.GetComponent<Button>().interactable = false;
        LeanTween.value(ui.score.gameObject, ui.score.color.a, 0f, 1f).setDelay(1f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = ui.score.color; textColor.a = alpha; ui.score.color = textColor; });
        LeanTween.scale(levelSuccess, new Vector3(1.2f, 1.2f, 1.2f), 1f).setDelay(3f).setEase(LeanTweenType.easeOutElastic);
        LeanTween.scale(sbad, new Vector3(1f, 1f, 1f), 1f).setDelay(4f).setEase(LeanTweenType.easeOutCubic);
        LeanTween.delayedCall(5.2f, () =>
        {
            tywr.timeBtwChars = 0.01f;
            tywr.StartTypewriter(15);
        });
        LeanTween.delayedCall(12f, () =>
        {
            ui.ds.GetComponent<Button>().interactable = true;
            Ad();
        });
    }

    public void AdInfoFinish()
    {
        ui.restartBtn.GetComponent<Button>().interactable = true;
        ui.htpBtn.GetComponent<Button>().interactable = true;
        ui.discordsw.GetComponent<Button>().interactable = true;
        LeanTween.value(ui.score.gameObject, ui.score.color.a, 1f, 0f).setEase(LeanTweenType.easeInOutQuad).setOnUpdate((float alpha) => { Color textColor = ui.score.color; textColor.a = alpha; ui.score.color = textColor; });
        ui.Tt.SetActive(false);
    }
}