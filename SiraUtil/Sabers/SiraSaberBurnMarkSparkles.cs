using UnityEngine;
using System.Linq;
using System.Reflection;
using SiraUtil.Interfaces;
using System.Collections.Generic;

namespace SiraUtil.Sabers
{
    public class SiraSaberBurnMarkSparkles : SaberBurnMarkSparkles, ISaberRegistrar
    {
        private readonly List<SaberBurnDatum> _saberBurnData = new List<SaberBurnDatum>();

        public SiraSaberBurnMarkSparkles()
        {
            SaberBurnMarkSparkles original = GetComponent<SaberBurnMarkSparkles>();
            foreach (FieldInfo info in original.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                info.SetValue(this, info.GetValue(original));
            }
            Destroy(original);
        }

		private bool _initted;

        public override void Start()
        {
			Run();
		}

		public void Initialize(SaberManager saberManager)
		{
			Run();
			_sabers = new Saber[2];
			_sabers[0] = saberManager.leftSaber;
			_sabers[1] = saberManager.rightSaber;
			_saberBurnData[0].saber = _sabers[0];
			_saberBurnData[1].saber = _sabers[1];
		}

		private void Run()
		{
			if (!_initted)
			{
				base.Start();
				_sabers[0] = _saberManager.leftSaber;
				_sabers[1] = _saberManager.rightSaber;
				for (int i = 0; i < 2; i++)
				{
					if (_sabers[i])
					{
						var saberBurnDatum = new SaberBurnDatum
						{
							saber = _sabers[i],
							color = _sabers[i].GetColor(),
							prevBurnMarkPos = _prevBurnMarkPos[i],
							burnMarkParticleSystem = _burnMarksPS[i],
							prevBurnMarkPosValid = _prevBurnMarkPosValid[i],
							burnMarkEmissionModule = _burnMarksEmissionModules[i]
						};
						_saberBurnData.Add(saberBurnDatum);
					}
				}
				_initted = true;
			}

			Color.RGBToHSV(_colorManager.ColorForSaberType(SaberType.SaberA), out float h, out float s, out _);
			var color = Color.HSVToRGB(h, s, 1f);
			_saberBurnData[0].burnMarkEmissionModule = _saberBurnData[0].burnMarkParticleSystem.emission;
			var mainModule = _saberBurnData[0].burnMarkParticleSystem.main;
			mainModule.startColor = color;
			_saberBurnData[0].color = color;

			Color.RGBToHSV(_colorManager.ColorForSaberType(SaberType.SaberB), out h, out s, out _);
			color = Color.HSVToRGB(h, s, 1f);
			_saberBurnData[1].burnMarkEmissionModule = _saberBurnData[1].burnMarkParticleSystem.emission;
			mainModule = _saberBurnData[1].burnMarkParticleSystem.main;
			mainModule.startColor = color;
			_saberBurnData[1].color = color;
		}

        public override void LateUpdate()
        {
            for (int i = 0; i < _saberBurnData.Count; i++)
            {
                Saber saber = _saberBurnData[i].saber;
                Color color = _saberBurnData[i].color;
                ParticleSystem particleSystem = _saberBurnData[i].burnMarkParticleSystem;
                ParticleSystem.EmissionModule emissionModule = _saberBurnData[i].burnMarkEmissionModule;

                var vector = new Vector3(0f, 0f, 0f);
                bool isActive = saber != null && saber.isActiveAndEnabled && GetBurnMarkPos(saber.saberBladeBottomPos, saber.saberBladeTopPos, out vector);
                if (isActive)
                {
                    particleSystem.transform.localPosition = vector;
                }
                if (isActive && !_saberBurnData[i].prevBurnMarkPosValid)
                {
                    emissionModule.enabled = isActive;
                }
                else if (!isActive && !_saberBurnData[i].prevBurnMarkPosValid)
                {
                    emissionModule.enabled = false;
                    particleSystem.Clear();
                }
                _sparklesEmitParams.startColor = color;
                if (isActive && _saberBurnData[i].prevBurnMarkPosValid)
                {
                    Vector3 a = vector - _saberBurnData[i].prevBurnMarkPos;
                    float magnitude = a.magnitude;
                    float num = 0.05f;
                    int num2 = (int)(magnitude / num);
                    int num3 = (num2 > 0) ? num2 : 1;
                    for (int j = 0; j <= num2; j++)
                    {
                        _sparklesEmitParams.position = _saberBurnData[i].prevBurnMarkPos + a * j / num3;
                        _sparklesPS.Emit(_sparklesEmitParams, 1);
                    }
                }
                _saberBurnData[i].prevBurnMarkPosValid = isActive;
                _saberBurnData[i].prevBurnMarkPos = vector;
            }
        }

        public override void OnDestroy()
        {
            for (int i = 0; i < _saberBurnData.Count; i++)
            {
                if (_saberBurnData[i].burnMarkParticleSystem != null)
                {
                    Destroy(_saberBurnData[i].burnMarkParticleSystem.gameObject);
                }
            }
        }

        public void RegisterSaber(Saber saber)
        {
            var newSaberDatum = new SaberBurnDatum
            {
                saber = saber,
                prevBurnMarkPos = default,
                prevBurnMarkPosValid = false,
                burnMarkParticleSystem = Instantiate(_burnMarksPSPrefab, Vector3.zero, Quaternion.Euler(270, 0, 0), null),
            };

            Color color = saber.GetColor();
            Color.RGBToHSV(color, out float h, out float s, out _);
            color = Color.HSVToRGB(h, s, 1f);
            newSaberDatum.burnMarkEmissionModule = newSaberDatum.burnMarkParticleSystem.emission;
            var mainModule = newSaberDatum.burnMarkParticleSystem.main;
            mainModule.startColor = color;
            newSaberDatum.color = color;

            _saberBurnData.Add(newSaberDatum);
        }

        public void UnregisterSaber(Saber saber)
        {
            SaberBurnDatum saberBurnDatum = _saberBurnData.Where(sbd => sbd.saber == saber).FirstOrDefault();
            if (saberBurnDatum != null)
            {
                if (saberBurnDatum.burnMarkParticleSystem != null)
                {
                    Destroy(saberBurnDatum.burnMarkParticleSystem.gameObject);
                }
                _saberBurnData.Remove(saberBurnDatum);
            }
        }

        private class SaberBurnDatum
        {
            public Saber saber;
            public Color color;
            public Vector3 prevBurnMarkPos;
            public bool prevBurnMarkPosValid;
            public ParticleSystem burnMarkParticleSystem;
            public ParticleSystem.EmissionModule burnMarkEmissionModule;
        }

        public void ChangeColor(Saber saber)
        {
            SaberBurnDatum saberBurnDatum = _saberBurnData.FirstOrDefault(sbd => sbd.saber == saber);
            if (saberBurnDatum != null)
            {
                Color color = saber.GetColor();
                Color.RGBToHSV(color, out float h, out float s, out _);
                color = Color.HSVToRGB(h, s, 1f);
                saberBurnDatum.burnMarkEmissionModule = saberBurnDatum.burnMarkParticleSystem.emission;
                var mainModule = saberBurnDatum.burnMarkParticleSystem.main;
                mainModule.startColor = color;
                saberBurnDatum.color = color;
            }
        }
	}
}