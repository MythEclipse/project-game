using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class BattleHUD : MonoBehaviour {
    Canvas _canvas;
    Text _turnTxt;
    Button[] _btns;
    Image[] _usedIcons; // overlay "used" indicator
    GameObject _sideRoot;
    Image _hpFill;
    Text _nameT, _clsT, _hpT, _acT;
    Text[] _statT;
    GameObject _endOver;

    static Color BAR = new Color(.06f,.04f,.02f,.95f);
    static Color PNL = new Color(.14f,.12f,.08f,.95f);

    // ═══════ UNITY UI LAYOUT HELPERS ═══════
    // Mirip HTML: div.flex-row, div.flex-col, div.fixed

    GameObject Make(string n, Transform p) {
        var g=new GameObject(n); g.transform.SetParent(p,false);
        g.AddComponent<RectTransform>();
        return g;
    }
    T Add<T>(GameObject g) where T:Component => g.AddComponent<T>();

    // Full stretch (fill parent)
    void Stretch(GameObject g) {
        var r=g.GetComponent<RectTransform>();
        r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one;
        r.offsetMin=Vector2.zero; r.offsetMax=Vector2.zero;
    }

    // Bar di tepi (0=bottom, 1=top), fixed height
    GameObject Bar(Transform p, float edge, float h) {
        var g=Make("bar",p);
        var r=g.GetComponent<RectTransform>();
        r.anchorMin=new Vector2(0,edge);
        r.anchorMax=new Vector2(1,edge);
        r.pivot=new Vector2(.5f,edge);
        r.sizeDelta=new Vector2(0,h);
        r.anchoredPosition=Vector2.zero;
        return g;
    }

    // Fixed position relative to corner
    GameObject Fixed(Transform p, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 pos) {
        var g=Make("f",p);
        var r=g.GetComponent<RectTransform>();
        r.anchorMin=anchor; r.anchorMax=anchor;
        r.pivot=pivot;
        r.sizeDelta=size;
        r.anchoredPosition=pos;
        return g;
    }

    // Horizontal container — anak-anaknya auto layout ke samping
    GameObject HBox(Transform p) {
        var g=Make("hbox",p);
        Stretch(g);
        var hl=g.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment=TextAnchor.MiddleCenter;
        hl.childForceExpandWidth=false;
        hl.childForceExpandHeight=true;
        hl.spacing=14;
        return g;
    }

    // Vertical container — auto layout ke bawah
    GameObject VBox(Transform p) {
        var g=Make("vbox",p);
        Stretch(g);
        var vl=g.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment=TextAnchor.UpperCenter;
        vl.childForceExpandWidth=true;
        vl.childForceExpandHeight=false;
        vl.spacing=4;
        vl.padding=new RectOffset(10,10,14,10);
        return g;
    }

    // Image background
    void BG(GameObject g, Color c) {
        var i=Add<Image>(g);
        i.color=c; i.raycastTarget=false;
    }

    // Background with subtle border gradient effect (two overlays)
    void StylePanel(GameObject p) {
        // Outer dark border effect via inset shadow color
        var outer=Make("outer",p.transform);
        Stretch(outer);
        var oi=Add<Image>(outer);
        oi.color=new Color(0.08f,0.06f,0.04f,0.4f); // subtle dark edge
        oi.raycastTarget=false;
        var or=outer.GetComponent<RectTransform>();
        or.offsetMin=new Vector2(-1,-1);
        or.offsetMax=new Vector2(1,1);
    }

    // Text stretch
    Text Txt(Transform p, string t, int sz, Color c, TextAnchor a) {
        var g=Make("t",p); Stretch(g);
        var tx=Add<Text>(g);
        tx.text=t; tx.fontSize=sz; tx.color=c; tx.alignment=a;
        tx.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tx.supportRichText=true; tx.raycastTarget=false;
        return tx;
    }

    // Button with fixed position
    Button Btn(Transform p, string l, float w, float h, Color bg, Color fg, int sz, float px, float py) {
        var g=Make("b",p);
        var r=g.GetComponent<RectTransform>();
        r.anchorMin=new Vector2(.5f,.5f); r.anchorMax=new Vector2(.5f,.5f);
        r.sizeDelta=new Vector2(w,h);
        r.anchoredPosition=new Vector2(px,py);
        var i=Add<Image>(g); i.color=bg; i.raycastTarget=true;
        var b=Add<Button>(g); b.targetGraphic=i;
        b.transition=Selectable.Transition.ColorTint;
        var cb=b.colors; cb.highlightedColor=Color.Lerp(bg,Color.white,.3f);
        cb.pressedColor=Color.Lerp(bg,Color.black,.3f); b.colors=cb;
        var nv=b.navigation; nv.mode=Navigation.Mode.None; b.navigation=nv;
        b.onClick.AddListener(()=>{});
        Txt(g.transform,l,sz,fg,TextAnchor.MiddleCenter);
        return b;
    }

    // HP Bar
    Image HPBar(Transform p, float w, float h) {
        var g=Make("hpbar",p);
        var r=g.GetComponent<RectTransform>();
        r.sizeDelta=new Vector2(w,h);
        BG(g,UIColorPalette.HpBg);
        var fg=Make("fill",g.transform);
        var fr=fg.GetComponent<RectTransform>();
        fr.anchorMin=Vector2.zero; fr.anchorMax=Vector2.one;
        fr.offsetMin=Vector2.one; fr.offsetMax=Vector2.one*-1;
        var fi=Add<Image>(fg);
        fi.type=Image.Type.Filled; fi.fillMethod=Image.FillMethod.Horizontal;
        fi.fillAmount=1f; fi.color=UIColorPalette.HpGreen; fi.raycastTarget=false;
        return fi;
    }

    // ─── BORDER ───
    static Sprite _brdSpr;
    static Sprite BorderSprite {
        get {
            if(_brdSpr!=null)return _brdSpr;
            var t=new Texture2D(3,3); t.filterMode=FilterMode.Point;
            for(int x=0;x<3;x++)for(int y=0;y<3;y++)
                t.SetPixel(x,y,(x==1&&y==1)?Color.clear:UIColorPalette.GoldTrim);
            t.Apply();
            _brdSpr=Sprite.Create(t,new Rect(0,0,3,3),new(.5f,.5f),1,0,SpriteMeshType.Tight,new Vector4(1,1,1,1));
            return _brdSpr;
        }
    }

    void AddBorder(GameObject p) {
        var g=Make("border",p.transform);
        Stretch(g);
        var i=Add<Image>(g);
        i.sprite=BorderSprite; i.type=Image.Type.Sliced; i.raycastTarget=false;
    }

    // ═══════ INIT ═══════

    public void Init() {
        // Canvas
        if(GameObject.FindObjectOfType<EventSystem>()==null){
            var g=new GameObject("ES",typeof(EventSystem));
            g.AddComponent<InputSystemUIInputModule>();
        }
        var cgo=new GameObject("HUD_Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        _canvas=cgo.GetComponent<Canvas>();
        _canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var sc=cgo.GetComponent<CanvasScaler>();
        sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution=new Vector2(1920,1080);
        sc.matchWidthOrHeight=0.5f;
        cgo.transform.SetParent(transform,false);

        // ───── TOP BAR ─────
        var top=Bar(cgo.transform,1,50);
        BG(top,BAR);
        _turnTxt=Txt(top.transform,"⚔ Battle",22,UIColorPalette.ParchmentLight,TextAnchor.MiddleCenter);

        // ───── BOTTOM BAR ─────
        var bot=Bar(cgo.transform,0,90);
        BG(bot,BAR);
        _btns=new Button[5];
        _usedIcons=new Image[4];
        string[] lbls={"Move","Attack","Defend","Wait","End Turn"};
        float[] bx={-240,-80,80,240,420};
        for(int i=0;i<5;i++){
            _btns[i]=Btn(bot.transform,lbls[i],i<4?110:120,44,
                i<4?UIColorPalette.StoneMid:UIColorPalette.DeepRed,Color.white,16,bx[i],0);
            // Used indicator overlay (untuk Move/Attack/Defend/Wait)
            if(i<4){
                var ogo=new GameObject("used",typeof(Image));
                var ort=ogo.GetComponent<RectTransform>();
                ort.SetParent(_btns[i].transform,false);
                ort.anchorMin=Vector2.zero;ort.anchorMax=Vector2.one;
                ort.sizeDelta=Vector2.zero;
                var oi=ogo.GetComponent<Image>();
                oi.color=new Color(0,0,0,0.5f);
                oi.raycastTarget=false;
                oi.gameObject.SetActive(false);
                // Checkmark
                var cgo2=new GameObject("check",typeof(Text));
                var crt=cgo2.GetComponent<RectTransform>();
                crt.SetParent(ogo.transform,false);
                crt.anchorMin=new Vector2(1,1);
                crt.anchorMax=new Vector2(1,1);
                crt.pivot=new Vector2(1,1);
                crt.sizeDelta=new Vector2(20,20);
                crt.anchoredPosition=new Vector2(-2,-2);
                var ctxt=cgo2.GetComponent<Text>();
                ctxt.text="✓";
                ctxt.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                ctxt.fontSize=16;
                ctxt.fontStyle=FontStyle.Bold;
                ctxt.color=Color.green;
                ctxt.alignment=TextAnchor.MiddleCenter;
                ctxt.raycastTarget=false;
                _usedIcons[i]=oi;
            }
        }

        // ───── SIDE PANEL ─────
        _sideRoot=Fixed(cgo.transform,Vector2.one,Vector2.one,new(230,320),new(-6,-56)).gameObject;
        var si=Add<Image>(_sideRoot);
        si.color=BAR; // same as bars
        si.raycastTarget=false;

        var vbox=Make("vbox",_sideRoot.transform);
        Stretch(vbox);
        var vl=vbox.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment=TextAnchor.UpperCenter;
        vl.childForceExpandWidth=true;
        vl.childForceExpandHeight=false;
        vl.spacing=2;
        vl.padding=new RectOffset(10,10,12,8);

        _nameT=Txt(vbox.transform,"---",22,UIColorPalette.ParchmentLight,TextAnchor.MiddleCenter);
        _clsT=Txt(vbox.transform,"",16,UIColorPalette.GoldTrim,TextAnchor.MiddleCenter);

        _hpFill=HPBar(vbox.transform,190,16);
        _hpT=Txt(vbox.transform,"HP: 0/0",15,Color.white,TextAnchor.MiddleCenter);
        _acT=Txt(vbox.transform,"AC: --",15,UIColorPalette.ParchmentLight,TextAnchor.MiddleCenter);

        _statT=new Text[6];
        string[] sn={"STR","DEX","CON","INT","WIS","CHA"};
        for(int i=0;i<6;i++){
            _statT[i]=Txt(vbox.transform,$"{sn[i]}: --",15,UIColorPalette.ParchmentDark,TextAnchor.MiddleCenter);
        }

        _sideRoot.SetActive(false);

        // ───── EVENTS ─────
        if(TurnManager.Instance!=null){
            TurnManager.Instance.OnPhaseChanged+=OnPhase;
            TurnManager.Instance.OnActiveUnitChanged+=OnUnit;
            TurnManager.Instance.OnBattleEnd+=OnEnd;
        }
        if(ActionSystem.Instance!=null){
            ActionSystem.Instance.OnAttackPerformed+=OnAtk;
            ActionSystem.Instance.OnActionExecuted+=OnAct;
        }
    }

    public void InitActions() {
        var bc=BattleController.Instance; if(bc==null)return;
        Bind(0,()=>bc.OnMoveButton());
        Bind(1,()=>bc.OnAttackButton());
        Bind(2,()=>bc.OnDefendButton());
        Bind(3,()=>bc.OnWaitButton());
        Bind(4,()=>{if(TurnManager.Instance?.ActiveUnit!=null)TurnManager.Instance.EndUnitTurn();});
    }
    void Bind(int i,UnityEngine.Events.UnityAction a){_btns[i].onClick.RemoveAllListeners();_btns[i].onClick.AddListener(a);}

    void OnPhase(BattlePhase p){
        if(p==BattlePhase.Battle)_turnTxt.text="⚔ Battle";
        else if(p==BattlePhase.Victory){_turnTxt.text="✦ VICTORY! ✦";_turnTxt.color=UIColorPalette.HpGreen;Hide();ShowEnd(true);}
        else if(p==BattlePhase.Defeat){_turnTxt.text="✟ DEFEAT...";_turnTxt.color=UIColorPalette.DeepRed;Hide();ShowEnd(false);}
    }

    void OnUnit(Unit u){
        if(u==null)return;
        _turnTxt.text=$"⚔ {u.Data?.unitName??"Unit"}'s Turn";
        _turnTxt.color=u.IsPlayerControlled?UIColorPalette.ParchmentLight:UIColorPalette.DeepRed;
        if(u.IsPlayerControlled&&TurnManager.Instance.CurrentPhase==BattlePhase.Battle)
            {ShowUnit(u);ShowBtns(true);Refr(u);InitActions();}
        else{_sideRoot.SetActive(false);Hide();}
    }

    void OnAtk(Unit a,Unit d,AttackResult r){
        var g=new GameObject("dp",typeof(DamagePopup),typeof(RectTransform));
        g.transform.SetParent(_canvas.transform,false);
        g.GetComponent<DamagePopup>().Init(d.transform.position,r);
        if(TurnManager.Instance?.ActiveUnit!=null)Refr(TurnManager.Instance.ActiveUnit);
    }
    void OnAct(Unit u,ActionType a){if(u!=null&&u.IsPlayerControlled)Refr(u);}
    void OnEnd(bool w){Hide();_sideRoot.SetActive(false);}

    void ShowBtns(bool v){foreach(var b in _btns)b.gameObject.SetActive(v);}
    void Hide(){ShowBtns(false);}
    void Refr(Unit u){
        _btns[0].interactable=!u.HasMovedThisTurn;
        _btns[1].interactable=!u.HasActedThisTurn;
        _btns[2].interactable=!u.HasActedThisTurn;
        _btns[3].interactable=!u.HasActedThisTurn;
        // Toggle used overlays
        if(_usedIcons==null||_usedIcons.Length<4)return;
        _usedIcons[0].gameObject.SetActive(u.HasMovedThisTurn);
        _usedIcons[1].gameObject.SetActive(u.HasActedThisTurn);
        _usedIcons[2].gameObject.SetActive(u.HasActedThisTurn);
        _usedIcons[3].gameObject.SetActive(u.HasActedThisTurn);
    }

    void ShowUnit(Unit u){
        if(u?.Data==null){_sideRoot.SetActive(false);return;}
        _sideRoot.SetActive(true);
        var d=u.Data; _nameT.text=d.unitName; _clsT.text=d.unitClass.ToString();
        float r=(float)u.CurrentHp/d.maxHp;
        _hpFill.fillAmount=Mathf.Clamp01(r); _hpFill.color=UIColorPalette.HpColor(r);
        _hpT.text=$"HP: {u.CurrentHp}/{d.maxHp}"; _acT.text=$"AC: {d.armorClass}";
        int[] st={d.strength,d.dexterity,d.constitution,d.intelligence,d.wisdom,d.charisma};
        string[] sn={"STR","DEX","CON","INT","WIS","CHA"};
        for(int i=0;i<6;i++){int m=d.GetAbilityModifier(st[i]);_statT[i].text=$"{sn[i]}: {st[i]} ({m:+0;-0;0})";}
    }

    void ShowEnd(bool w){
        if(_endOver!=null)Destroy(_endOver);
        _endOver=new GameObject("end",typeof(Image));
        var vr=_endOver.GetComponent<RectTransform>();
        vr.SetParent(_canvas.transform,false);
        vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;
        vr.sizeDelta=Vector2.zero;vr.anchoredPosition=Vector2.zero;
        _endOver.GetComponent<Image>().color=new Color(0,0,0,.75f);
        Txt(_endOver.transform,w?"✦ VICTORY! ✦":"✟ DEFEAT...",64,w?UIColorPalette.HpGreen:UIColorPalette.DeepRed,TextAnchor.MiddleCenter);
    }

    void OnDestroy(){
        if(TurnManager.Instance!=null){
            TurnManager.Instance.OnPhaseChanged-=OnPhase;
            TurnManager.Instance.OnActiveUnitChanged-=OnUnit;
            TurnManager.Instance.OnBattleEnd-=OnEnd;
        }
        if(ActionSystem.Instance!=null){
            ActionSystem.Instance.OnAttackPerformed-=OnAtk;
            ActionSystem.Instance.OnActionExecuted-=OnAct;
        }
    }
}
