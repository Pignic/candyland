using EldmeresTale.Core.UI;
using EldmeresTale.Systems.VFX;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.Systems;

public class VFXSystem : GameSystem {
	private readonly BitmapFont _font;
	private readonly List<DamageNumber> _damageNumbers;
	private readonly List<LevelUpEffect> _levelUpEffects;

	public int DamageNumberCount => _damageNumbers.Count;
	public int LevelUpEffectCount => _levelUpEffects.Count;

	public VFXSystem(BitmapFont font) : base() {
		_font = font;
		_damageNumbers = [];
		_levelUpEffects = [];
		Enabled = true;
		Visible = true;
	}

	public override void Initialize() {
		System.Diagnostics.Debug.WriteLine("[VFX SYSTEM] Initialized");
		base.Initialize();
	}

	public void ShowDamage(int amount, Vector2 position) {
		ShowDamage(amount, position, false, Color.Red);
	}

	public void ShowHealing(int amount, Vector2 position) {
		ShowDamage(amount, position, false, Color.Green);
	}

	public void ShowDamage(int amount, Vector2 position, bool isCrit, Color color) {
		_damageNumbers.Add(new DamageNumber(amount, position, _font, isCrit, color));
	}

	public void ShowLevelUp(Vector2 position) {
		_levelUpEffects.Add(new LevelUpEffect(position, _font));
	}

	public override void Update(GameTime gameTime) {
		if (!Enabled) {
			return;
		}

		// Update damage numbers
		foreach (DamageNumber damageNumber in _damageNumbers) {
			damageNumber.Update(gameTime);
		}

		// Remove expired damage numbers
		_damageNumbers.RemoveAll(d => d.IsExpired);

		// Update level up effects
		foreach (LevelUpEffect effect in _levelUpEffects) {
			effect.Update(gameTime);
		}

		// Remove expired level up effects
		_levelUpEffects.RemoveAll(e => e.IsExpired);

		base.Update(gameTime);
	}

	public override void Draw(SpriteBatch spriteBatch) {
		if (!Visible) {
			return;
		}

		// Draw damage numbers
		foreach (DamageNumber damageNumber in _damageNumbers) {
			damageNumber.Draw(spriteBatch);
		}

		// Draw level up effects
		foreach (LevelUpEffect effect in _levelUpEffects) {
			effect.Draw(spriteBatch);
		}

		base.Draw(spriteBatch);
	}

	public override void Dispose() {
		_damageNumbers.Clear();
		_levelUpEffects.Clear();
		System.Diagnostics.Debug.WriteLine("[VFX SYSTEM] Disposed");
		base.Dispose();
	}

	public void Clear() {
		_damageNumbers.Clear();
		_levelUpEffects.Clear();
	}
}