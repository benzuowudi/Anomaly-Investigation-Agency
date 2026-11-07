using DG.Tweening;
using Framework.Script.Base;
using UnityEngine;

namespace Framework.Script.Helper
{
    public class GameHelper : Singleton<GameHelper>
    {
        public void Pan(Transform _transform, Vector3 _end, float _speed)
        {
            _transform.DOLocalMove(_end, _speed);
        }

        public void Reveal(Transform _transform, float _end, float _speed)
        {
            DOTween.To(() => _transform.GetComponent<CanvasGroup>().alpha, x => _transform.GetComponent<CanvasGroup>().alpha = x, _end, _speed);
        }

        public void Deformation(Transform _transform, Vector2 _end, float _speed)
        {
            DOTween.To(() => _transform.GetComponent<RectTransform>().sizeDelta, x => _transform.GetComponent<RectTransform>().sizeDelta = x, _end, _speed);
        }
    }
}