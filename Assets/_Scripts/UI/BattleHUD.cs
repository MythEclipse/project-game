using UnityEngine;
using UnityEngine.UI;

public class BattleHUD : MonoBehaviour {
    private Canvas _canvas;

    // Bars
    private Image _topBar, _botBar;

    // Side panel & children
    private Image _sidePanel;
    private Text _turnLabel;
    private Text _nameText, _classText, _hpText, _acText;
    private Image _hpBarFill, _hpBarBg;
    private Text[] _statTexts;
    private Button[] _actionBtns;
    private GameObject _victoryOverlay;

    static readonly Color BarBg  = new Color(0.06f, 0.04f, 0.02f, 0.95f);
    static readonly Color PnlBg  = new Color(0.14f, 0.12f, 0.08f, 0.95f);
    const float SideW = 240;
    const float TopH  = 50;
    const float BotH  = 90;

    public void Init() {
        // ── CANVAS ──
        _canvas = LayoutHelper.CreateCanvas("BattleHUD_Canvas");
        _canvas.transform.SetParent(transform, false);
        var cRt = _canvas.GetComponent<RectTransform>();

        // ── TOP BAR (50px) ──
        _topBar = LayoutHelper.CreateTopBar(cRt.transform, TopH, BarBg);
        _turnLabel = LayoutHelper.CreateText(_topBar.transform, "Battle", 22,
            UIColorPalette.ParchmentLight);

        // ── BOTTOM BAR (90px) ──
        _botBar = LayoutHelper.CreateBottomBar(cRt.transform, BotH, BarBg);

        string[] btnLabels = {"Move","Attack","Defend","Wait","End Turn"};
        float[] btnX = {-240,-80,80,240,420};
        float[] btnW = {130,130,130,130,140};
        Color[] btnBg = {UIColorPalette.StoneMid,UIColorPalette.StoneMid,
                         UIColorPalette.StoneMid,UIColorPalette.StoneMid,
                         UIColorPalette.DeepRed};
        _actionBtns = new Button[5];
        for (int i = 0; i < 5; i++) {
            var (_, btn) = LayoutHelper.CreateButton(_botBar.transform, btnLabels[i],
                btnW[i], 48, btnBg[i], Color.white, 16, btnX[i], 0, () => {});
            _actionBtns[i] = btn;
        }

        // ── SIDE PANEL (kanan, 240px, full height) ──
        _sidePanel = LayoutHelper.CreateRightPanel(cRt.transform, SideW, 8, PnlBg);
        var sp = _sidePanel.rectTransform;

        // Gold border
        var border = new GameObject("Border", typeof(Image));
        var bRt = border.GetComponent<RectTransform>();
        bRt.SetParent(sp, false);
        bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
        bRt.sizeDelta = Vector2.zero; bRt.anchoredPosition = Vector2.zero;
        var bImg = border.GetComponent<Image>(); bImg.raycastTarget = false;
        var tex = new Texture2D(4,4);
        for (int bx=0;bx<4;bx++) for(int by=0;by<4;by++)
            tex.SetPixel(bx,by,(bx<1||bx>=3||by<1||by>=3)?UIColorPalette.GoldTrim:Color.clear);
        tex.Apply();
        bImg.sprite = Sprite.Create(tex,new Rect(0,0,4,4),Vector2.one*0.5f,1);

        // ── SIDE PANEL CONTENT ──
        // Semua child anchor (0.5,1), pivot (0.5,1) — dihitung dari ATAS panel
        float cy = -16;
        _nameText = LayoutHelper.CreateTextFromTop(sp, "---", 20, UIColorPalette.ParchmentLight,
            0, cy, 220, 22, TextAnchor.UpperCenter); cy -= 24;

        _classText = LayoutHelper.CreateTextFromTop(sp, "", 14, UIColorPalette.GoldTrim,
            0, cy, 220, 20, TextAnchor.UpperCenter); cy -= 30;

        var (hpBg, hpFill) = LayoutHelper.CreateHPBar(sp, 210, 18, cy);
        _hpBarBg = hpBg; _hpBarFill = hpFill; cy -= 22;

        _hpText = LayoutHelper.CreateTextFromTop(sp, "HP: 0/0", 14, Color.white,
            0, cy, 210, 18, TextAnchor.MiddleCenter); cy -= 24;

        _acText = LayoutHelper.CreateTextFromTop(sp, "AC: --", 16, UIColorPalette.ParchmentLight,
            -90, cy, 200, 20, TextAnchor.MiddleLeft); cy -= 24;

        LayoutHelper.CreateSeparator(sp, 220, cy); cy -= 22;

        string[] sn = {"STR","DEX","CON","INT","WIS","CHA"};
        _statTexts = new Text[6];
        for (int i=0;i<6;i++) {
            float sx = (i%2==0) ? -100 : -10;
            float sy = cy - (i/2)*24;
            _statTexts[i] = LayoutHelper.CreateTextFromTop(sp,
                $"{sn[i]}: --", 14, UIColorPalette.ParchmentDark,
                sx, sy, 180, 20, TextAnchor.MiddleLeft);
        }

        _sidePanel.gameObject.SetActive(false);

        // ── EVENTS ──
        if (TurnManager.Instance != null) {
            TurnManager.Instance.OnPhaseChanged += OnPhaseChanged;
            TurnManager.Instance.OnActiveUnitChanged += OnActiveUnitChanged;
            TurnManager.Instance.OnBattleEnd += OnBattleEnd;
        }
        if (ActionSystem.Instance != null) {
            ActionSystem.Instance.OnAttackPerformed += OnAttackPerformed;
            ActionSystem.Instance.OnActionExecuted += OnActionExecuted;
        }
    }

    public void InitActions() {
        _actionBtns[0].onClick.RemoveAllListeners();
        _actionBtns[0].onClick.AddListener(()=>BattleController.Instance?.OnMoveButton());
        _actionBtns[1].onClick.RemoveAllListeners();
        _actionBtns[1].onClick.AddListener(()=>BattleController.Instance?.OnAttackButton());
        _actionBtns[2].onClick.RemoveAllListeners();
        _actionBtns[2].onClick.AddListener(()=>BattleController.Instance?.OnDefendButton());
        _actionBtns[3].onClick.RemoveAllListeners();
        _actionBtns[3].onClick.AddListener(()=>BattleController.Instance?.OnWaitButton());
        _actionBtns[4].onClick.RemoveAllListeners();
        _actionBtns[4].onClick.AddListener(()=>{
            if(TurnManager.Instance?.ActiveUnit!=null) TurnManager.Instance.EndUnitTurn();
        });
    }

    void OnPhaseChanged(BattlePhase p) {
        switch(p){
            case BattlePhase.Battle: _turnLabel.text="⚔ Battle"; break;
            case BattlePhase.Victory: _turnLabel.text="✦ VICTORY! ✦"; _turnLabel.color=UIColorPalette.HpGreen; HideAll(); ShowEnd(true); break;
            case BattlePhase.Defeat: _turnLabel.text="✟ DEFEAT..."; _turnLabel.color=UIColorPalette.DeepRed; HideAll(); ShowEnd(false); break;
        }
    }

    void OnActiveUnitChanged(Unit u) {
        if(u==null) return;
        _turnLabel.text = $"⚔ {u.Data?.unitName??"Unit"}'s Turn";
        _turnLabel.color = u.IsPlayerControlled ? UIColorPalette.ParchmentLight : UIColorPalette.DeepRed;

        if(u.IsPlayerControlled && TurnManager.Instance.CurrentPhase==BattlePhase.Battle) {
            DisplayUnit(u); ShowBtns(true); RefreshBtns(u); InitActions();
        } else {
            _sidePanel.gameObject.SetActive(false); ShowBtns(false);
        }
    }

    void OnAttackPerformed(Unit a, Unit d, AttackResult r) {
        var g=new GameObject("DP",typeof(DamagePopup));
        g.transform.SetParent(_canvas.transform,false);
        g.GetComponent<DamagePopup>().Init(d.transform.position,r);
        if(TurnManager.Instance?.ActiveUnit!=null) RefreshBtns(TurnManager.Instance.ActiveUnit);
    }

    void OnActionExecuted(Unit u, ActionType a) { if(u!=null&&u.IsPlayerControlled) RefreshBtns(u); }
    void OnBattleEnd(bool w) { HideAll(); _sidePanel.gameObject.SetActive(false); }

    void ShowBtns(bool v) { foreach(var b in _actionBtns) b.gameObject.SetActive(v); }
    void HideAll() { ShowBtns(false); }

    void RefreshBtns(Unit u) {
        _actionBtns[0].interactable=!u.HasMovedThisTurn;
        _actionBtns[1].interactable=!u.HasActedThisTurn;
        _actionBtns[2].interactable=!u.HasActedThisTurn;
        _actionBtns[3].interactable=!u.HasActedThisTurn;
    }

    void DisplayUnit(Unit u) {
        if(u?.Data==null){_sidePanel.gameObject.SetActive(false);return;}
        _sidePanel.gameObject.SetActive(true);
        var d=u.Data;
        _nameText.text=d.unitName;
        _classText.text=d.unitClass.ToString();
        float r=(float)u.CurrentHp/d.maxHp;
        _hpBarFill.fillAmount=Mathf.Clamp01(r);
        _hpBarFill.color=UIColorPalette.HpColor(r);
        _hpText.text=$"HP: {u.CurrentHp}/{d.maxHp}";
        _acText.text=$"AC: {d.armorClass}";
        int[] st={d.strength,d.dexterity,d.constitution,d.intelligence,d.wisdom,d.charisma};
        string[] sn={"STR","DEX","CON","INT","WIS","CHA"};
        for(int i=0;i<6;i++){int m=d.GetAbilityModifier(st[i]);_statTexts[i].text=$"{sn[i]}: {st[i]} ({m:+0;-0;0})";}
    }

    void ShowEnd(bool v) {
        if(_victoryOverlay!=null) Destroy(_victoryOverlay);
        _victoryOverlay=new GameObject("End",typeof(Image));
        var vr=_victoryOverlay.GetComponent<RectTransform>();
        vr.SetParent(_canvas.transform,false);
        vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;
        vr.sizeDelta=Vector2.zero;vr.anchoredPosition=Vector2.zero;
        _victoryOverlay.GetComponent<Image>().color=new Color(0,0,0,0.75f);
        LayoutHelper.CreateText(_victoryOverlay.transform,
            v?"✦ VICTORY! ✦":"✟ DEFEAT...",64,
            v?UIColorPalette.HpGreen:UIColorPalette.DeepRed);
    }

    void OnDestroy() {
        if(TurnManager.Instance!=null){
            TurnManager.Instance.OnPhaseChanged-=OnPhaseChanged;
            TurnManager.Instance.OnActiveUnitChanged-=OnActiveUnitChanged;
            TurnManager.Instance.OnBattleEnd-=OnBattleEnd;
        }
        if(ActionSystem.Instance!=null){
            ActionSystem.Instance.OnAttackPerformed-=OnAttackPerformed;
            ActionSystem.Instance.OnActionExecuted-=OnActionExecuted;
        }
    }
}
