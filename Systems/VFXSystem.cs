using EldmeresTale.Core.UI;
using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.Systems;

public class VFXSystem : GameSystem {
	private readonly BitmapFont _font;
	private readonly List<DamageNumber> _damageNumbers;
	private readonly List<LevelUpEffect> _levelUpEffects;

	public VFXSystem(BitmapFont font) : base() {
		_font = font;
		_damageNumbers = new List<DamageNumber>();
		_levelUpEffects = new List<LevelUpEffect>();
		Enabled = true;
		Visible = true;
	}

	public override void Initialize() {
		System.Diagnostics.Debug.WriteLine("[VFX SYSTEM] Initialized");
		base.Initialize();
	}

	/// <summary>
	/// Show a damage number at the specified position
	/// </summary>
	public void ShowDamage(int amount, Vector2 position, bool isCrit) {
		Color color = isCrit ? Color.Yellow : Color.White;
		_damageNumbers.Add(new DamageNumber(amount, position, _font, isCrit, color));
	}

	public void ShowHealing(int amount, Vector2 position) {
		_damageNumbers.Add(new DamageNumber(amount, position, _font, false, Color.LimeGreen));
	}

	/// <summary>
	/// Show a damage number with custom color
	/// </summary>
	public void ShowDamage(int amount, Vector2 position, bool isCrit, Color color) {
		_damageNumbers.Add(new DamageNumber(amount, position, _font, isCrit, color));
	}

	/// <summary>
	/// Show a level up effect at the specified position
	/// </summary>
	public void ShowLevelUp(Vector2 position) {
		_levelUpEffects.Add(new LevelUpEffect(position, _font));
	}

	public override void Update(GameTime gameTime) {
		if(!Enabled) return;

		// Update damage numbers
		foreach(var damageNumber in _damageNumbers) {
			damageNumber.Update(gameTime);
		}

		// Remove expired damage numbers
		_damageNumbers.RemoveAll(d => d.IsExpired);

		// Update level up effects
		foreach(var effect in _levelUpEffects) {
			effect.Update(gameTime);
		}

		// Remove expired level up effects
		_levelUpEffects.RemoveAll(e => e.IsExpired);

		base.Update(gameTime);
	}

	public override void Draw(SpriteBatch spriteBatch) {
		if(!Visible) return;

		// Draw damage numbers
		foreach(var damageNumber in _damageNumbers) {
			damageNumber.Draw(spriteBatch);
		}

		// Draw level up effects
		foreach(var effect in _levelUpEffects) {
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

	/// <summary>
	/// Clear all active effects
	/// </summary>
	public void Clear() {
		_damageNumbers.Clear();
		_levelUpEffects.Clear();
	}

	/// <summary>
	/// Get the current count of active damage numbers
	/// </summary>
	public int DamageNumberCount => _damageNumbers.Count;

	/// <summary>
	/// Get the current count of active level up effects
	/// </summary>
	public int LevelUpEffectCount => _levelUpEffects.Count;
}