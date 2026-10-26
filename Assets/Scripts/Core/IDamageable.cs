/**FileHeader
 * @Author: Federico Marazzi
 * @Date: 9/21/2026, 12:59:28 PM
 * @LastEditors: Federico Marazzi
 * @LastEditTime: 10/26/2026, 3:55:00 PM
 * @Description: Contrato de todo lo que puede recibir daño. Lo traje del proyecto anterior sin cambios.
 * @Copyright: Copyright (©)}) 2026 Federico Marazzi. All rights reserved.
 * @Email: federicoandresmarazzi@gmail.com
 */

// Todo lo que pueda recibir daño implementa esto: el jugador, los enemigos, los aliados, las jaulas...
// Así las balas y los golpes no necesitan saber qué es lo que están pegando, solo que es dañable
public interface IDamageable
{
    void TakeDamage(float amount); // le saco vida
    float CurrentHealth { get; }   // cuánta vida tiene ahora (desde afuera solo se lee)
    bool IsDead { get; }           // sirve para saber si ya está muerto
}
