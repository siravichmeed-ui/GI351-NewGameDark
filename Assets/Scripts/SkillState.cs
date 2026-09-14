using UnityEngine;
using System;

public class SkillState : MonoBehaviour
{
    [Header("Skill Cooldown")]
    [SerializeField] private float baseCooldown = 10f;

    private float currentCooldown;

    public float BaseCooldown => baseCooldown;
    public float CurrentCooldown => currentCooldown;

    // true = พร้อมใช้
    public bool IsReady { get; private set; }

    // 0 - 1
    // เอาไปทำหลอด Cooldown UI ได้
    public float CooldownPercent
    {
        get
        {
            if (baseCooldown <= 0f)
                return 1f;

            return 1f - (currentCooldown / baseCooldown);
        }
    }

    public event Action<float> OnCooldownChanged;
    public event Action OnSkillReady;

    private void Awake()
    {
        // เริ่มเกมให้ใช้ Skill ได้เลย
        IsReady = true;
        currentCooldown = 0f;
    }

    private void Update()
    {
        if (IsReady)
            return;

        currentCooldown -= Time.deltaTime;

        if (currentCooldown <= 0f)
        {
            currentCooldown = 0f;
            IsReady = true;

            OnCooldownChanged?.Invoke(currentCooldown);
            OnSkillReady?.Invoke();
        }
        else
        {
            OnCooldownChanged?.Invoke(currentCooldown);
        }
    }

    // เรียกเมื่อใช้ Skill
    public bool TryUseSkill()
    {
        if (!IsReady)
            return false;

        IsReady = false;
        currentCooldown = baseCooldown;

        OnCooldownChanged?.Invoke(currentCooldown);

        return true;
    }

    // ลด Cooldown จากระบบ Upgrade ในอนาคต
    public void ReduceCooldown(float amount)
    {
        baseCooldown -= amount;

        // กันไม่ให้ต่ำกว่า 0.1 วิ
        baseCooldown = Mathf.Max(0.1f, baseCooldown);
    }

    // ตั้ง Cooldown ใหม่โดยตรง
    public void SetCooldown(float newCooldown)
    {
        baseCooldown = Mathf.Max(0.1f, newCooldown);
    }
}