using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    public static PlayerStateManager Instance;

    public float maxHealth = 100f;
    public float currentHealth;

    public float maxStamina = 100f;
    public float currentStamina;

    public int currentCoins;

    public float staminaDrainRate = 10f;
    public float staminaRegenRate = 5f;

    public bool isFrozen = false;

    public bool CanMove => currentHealth > 0 && !isFrozen;

    private float levelStartHealth;
    private float levelStartStamina;
    private int levelStartCoins;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currentCoins = 0;

        SaveLevelStartState();
    }

    private void Update()
    {
        if (!Input.GetKey(KeyCode.LeftShift) || !IsMoving())
        {
            RegenerateStamina();
        }
    }

    private bool IsMoving()
    {
        return Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;
    }

    private void RegenerateStamina()
    {
        currentStamina += staminaRegenRate * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("💀 Jugador ha mort!");
            GameManager.Instance.GoToGameOver();
        }
    }

    public bool TryUseStamina(float amount)
    {
        if (currentStamina >= amount && IsMoving())
        {
            currentStamina -= amount;
            return true;
        }
        return false;
    }

    public void AddCoins(int amount)
    {
        currentCoins += amount;
    }

    public void SetPlayerState(float health, float stamina, int coins)
    {
        currentHealth = health;
        currentStamina = stamina;
        currentCoins = coins;
    }

    public void SaveLevelStartState()
    {
        levelStartHealth = currentHealth;
        levelStartStamina = currentStamina;
        levelStartCoins = currentCoins;

        Debug.Log($"📝 Estat inicial guardat ➜ Vida: {levelStartHealth}, Estamina: {levelStartStamina}, Monedes: {levelStartCoins}");
    }

    public void RestoreLevelStartState()
    {
        SetPlayerState(levelStartHealth, levelStartStamina, levelStartCoins);
        Debug.Log($"🔄 Estat inicial restaurat ➜ Vida: {currentHealth}, Estamina: {currentStamina}, Monedes: {currentCoins}");
    }
}
