/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 9/21/2026, 12:59:47 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 10/26/2026, 3:55:00 PM
 * @Description: Clase base con vida. La traje del proyecto anterior.
 *   CAMBIOS: ya no llamo al UIManager desde acá, ahora aviso con eventos y quien quiera se suscribe.
 *   Agregué Heal() para los botiquines y antídotos, y ResetHealth() para el respawn.
 * @Copyright: Copyright (©)}) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */
using System;
using UnityEngine;

// Clase base de todo lo que tiene vida (jugador, enemigos, aliados).
// Es abstract porque nunca la uso sola, solo la heredo
public abstract class Entity : MonoBehaviour, IDamageable
{
    [SerializeField] protected float _maxHealth = 100f;

    protected float _currentHealth;
    protected bool _isDead;

    // Dejo la vida pública pero solo de lectura para que otros scripts la puedan mirar
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public bool IsDead => _isDead;

    // CAMBIO: antes le hablaba directo al UIManager; ahora aviso con eventos
    // y la UI o el GameManager se suscriben si les interesa
    public event Action<float, float> OnHealthChanged; // vida actual y vida máxima
    public event Action<Entity> OnDied;

    protected virtual void Awake()
    {
        _currentHealth = _maxHealth;
        _isDead = false;
    }

    // La lógica de daño es la misma para todos, las hijas la pueden pisar con override
    public virtual void TakeDamage(float amount)
    {
        if (_isDead || amount <= 0f) return;

        _currentHealth -= amount;
        _currentHealth = Mathf.Max(_currentHealth, 0f); // que no baje de 0
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        OnDamaged(amount); // gancho para efectos al recibir daño (parpadeo, sonido, etc.)

        if (_currentHealth <= 0f)
        {
            _isDead = true;
            OnDied?.Invoke(this);
            Die();
        }
    }

    // CAMBIO: nuevo. Sirve para botiquines y antídotos. Si ya está muerto no lo revive
    public virtual void Heal(float amount)
    {
        if (_isDead || amount <= 0f) return;

        _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    // CAMBIO: nuevo. Lo uso en el respawn del jugador para dejarlo con la vida llena
    protected void ResetHealth()
    {
        _isDead = false;
        _currentHealth = _maxHealth;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    // Cada hija decide cómo muere: el jugador hace respawn, el enemigo desaparece
    protected abstract void Die();

    // Las hijas lo pisan si quieren agregar algo cuando reciben daño
    protected virtual void OnDamaged(float amount) { }
}
