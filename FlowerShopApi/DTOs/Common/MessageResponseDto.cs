namespace FlowerShopApi.DTOs.Common
{
    /// <summary>
    /// Стандартна відповідь API, що містить текстове повідомлення.
    /// </summary>
    public class MessageResponseDto
    {
        /// <summary>
        /// Повідомлення про результат виконання операції.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}