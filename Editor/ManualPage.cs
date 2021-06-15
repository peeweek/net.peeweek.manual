using UnityEngine;

namespace ManualUtility
{
    [CreateAssetMenu(menuName = "Manual/Page", fileName = "New Manual Page", order = 0)]
    public class ManualPage : ScriptableObject
    {
        [Delayed]
        public string pageName = "New Page";

        public PageContent[] contents;

        public ManualPage[] subPages;

        [System.Serializable]
        public struct PageContent
        {
            public enum PageContentType
            {
                Paragraph,
                Header1,
                Header2,
                Header3,
                List,
                Image,
                Links,
            }

            public PageContentType contentType;

            [Multiline]
            public string[] lines;

        }
    }
}



