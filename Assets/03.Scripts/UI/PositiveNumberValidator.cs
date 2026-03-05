using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_InputField))]
public class PositiveNumberValidator : MonoBehaviour
{
    private TMP_InputField _inputField;

    void Start()
    {
        _inputField = GetComponent<TMP_InputField>();

        // ContentType을 실수형으로 세팅 (숫자, ., - 허용)
        _inputField.contentType = TMP_InputField.ContentType.DecimalNumber;

        // 값이 바뀔 때마다 체크하는 이벤트 연결
        _inputField.onValueChanged.AddListener(ValidatePositiveRealNumber);
    }

    private void ValidatePositiveRealNumber(string text)
    {
        // 마이너스 기호가 있으면 없애버리기
        if (text.Contains("-"))
        {
            _inputField.text = text.Replace("-", "");
        }
    }
}