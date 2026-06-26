using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ActionType {
    Move,
    Attack,
    Spell,
    Defend,
    Wait
}

public class ActionSystem : MonoBehaviour {
    public static ActionSystem Instance { get; private set; }

    public event Action<Unit, ActionType> OnActionExecuted;
    public event Action<Unit, Unit, AttackResult> OnAttackPerformed;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ===== MOVEMENT =====

    public IEnumerator ExecuteMove(Unit unit, Tile targetTile) {
        if (unit == null || targetTile == null) yield break;
        if (!targetTile.IsWalkable) yield break;

        // Remove dari tile lama
        GridManager.Instance.RemoveUnit(unit);

        // Animasi lerp
        Vector3 startPos = unit.transform.position;
        Vector3 endPos = targetTile.transform.position;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Smooth step
            t = t * t * (3f - 2f * t);
            unit.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        unit.transform.position = endPos;
        GridManager.Instance.PlaceUnit(unit, targetTile.GridPos);

        unit.HasMovedThisTurn = true;
        OnActionExecuted?.Invoke(unit, ActionType.Move);
    }

    // ===== ATTACK =====

    public void ExecuteAttack(Unit attacker, Unit defender) {
        if (attacker == null || defender == null) return;

        var result = DamageCalculator.CalculateDamage(attacker, defender);

        if (result.hit && result.damage > 0) {
            defender.TakeDamage(result.damage);
        }

        attacker.HasActedThisTurn = true;
        OnActionExecuted?.Invoke(attacker, ActionType.Attack);
        OnAttackPerformed?.Invoke(attacker, defender, result);

        Debug.Log($"{attacker.Data?.unitName ?? "Unit"} attacks {defender.Data?.unitName ?? "Target"}: {result.log}");
    }

    // ===== DEFEND =====

    public void ExecuteDefend(Unit unit) {
        if (unit == null) return;
        unit.IsDefending = true;
        unit.HasActedThisTurn = true;
        OnActionExecuted?.Invoke(unit, ActionType.Defend);
        Debug.Log($"{unit.Data?.unitName ?? "Unit"} is defending! (+4 AC)");
    }

    // ===== WAIT =====

    public void ExecuteWait(Unit unit) {
        if (unit == null) return;

        if (!unit.HasMovedThisTurn && !unit.HasActedThisTurn) {
            // Kalau belum ngapa-ngapain, skip turn entirely
        }

        unit.HasActedThisTurn = true;
        OnActionExecuted?.Invoke(unit, ActionType.Wait);
    }

    // ===== HELPERS =====

    public bool CanMove(Unit unit) => !unit.HasMovedThisTurn;
    public bool CanAct(Unit unit) => !unit.HasActedThisTurn;

    public bool CanAttackTarget(Unit attacker, Tile targetTile) {
        if (targetTile.Occupant == null) return false;
        if (!targetTile.Occupant.IsAlive) return false;
        if (attacker.Data == null) return true;

        var origin = attacker.GridPos;
        var target = targetTile.GridPos;
        int range = attacker.Data.attackRange;
        int dist = Mathf.Abs(origin.x - target.x) + Mathf.Abs(origin.y - target.y);
        return dist <= range && dist > 0;
    }
}
