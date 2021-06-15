using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ManualUtility
{
    [CreateAssetMenu(menuName ="Manual/Manual", fileName = "New Manual", order = 0)]
    public class Manual : ScriptableObject
    {
        [Delayed]
        public string manualName = "Manual";

        public ManualPage[] pages;

        private void OnValidate()
        {
            ManualWindow.Reload();
        }
    }
}



