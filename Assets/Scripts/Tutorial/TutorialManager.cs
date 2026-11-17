using System;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Tutorial
{








    public sealed class TutorialManager : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static TutorialManager Instance { get; private set; }

        [SerializeField] bool[] _pressedOk = new bool[6];

        public bool HasSeen(int frameIdx)
        {
            if (frameIdx < 0 || frameIdx >= _pressedOk.Length) return true;
            return _pressedOk[frameIdx];
        }



        public event Action<int> OnRequestShowFrame;
        public event Action OnTutorialFinished;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }



        public void MaybeShowOnBoot()
        {
            MaybeShow(0);
        }



        public void MaybeShow(int frameIdx)
        {
            if (frameIdx < 0 || frameIdx >= _pressedOk.Length) return;
            if (_pressedOk[frameIdx]) return;
            OnRequestShowFrame?.Invoke(frameIdx);
        }

        public void MarkSeen(int frameIdx)
        {
            if (frameIdx < 0 || frameIdx >= _pressedOk.Length) return;
            if (_pressedOk[frameIdx]) return;
            _pressedOk[frameIdx] = true;


            for (int i = 0; i < _pressedOk.Length; i++) if (!_pressedOk[i]) return;
            OnTutorialFinished?.Invoke();
        }


        public void ResetSeen()
        {
            for (int i = 0; i < _pressedOk.Length; i++) _pressedOk[i] = false;
        }


        public void LoadData(GameDataDTO data)
        {
            if (data == null) return;

            if (data.tutorialPressedOk != null && data.tutorialPressedOk.Length >= _pressedOk.Length)
            {
                for (int i = 0; i < _pressedOk.Length; i++) _pressedOk[i] = data.tutorialPressedOk[i];
            }
            else if (data.tutorialSeen)
            {

                _pressedOk[0] = true;
            }




        }

        public void SaveData(GameDataDTO data)
        {
            if (data == null) return;
            if (data.tutorialPressedOk == null || data.tutorialPressedOk.Length != _pressedOk.Length)
                data.tutorialPressedOk = new bool[_pressedOk.Length];
            for (int i = 0; i < _pressedOk.Length; i++) data.tutorialPressedOk[i] = _pressedOk[i];

            data.tutorialSeen = _pressedOk[0];
        }
    }
}
