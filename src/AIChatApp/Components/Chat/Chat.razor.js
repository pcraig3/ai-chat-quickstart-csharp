export function submitOnEnter(formElem) {
  formElem.addEventListener("keydown", (e) => {
    if (
      e.key === "Enter" &&
      !e.ctrlKey &&
      !e.shiftKey &&
      !e.altKey &&
      !e.metaKey
    ) {
      e.srcElement.dispatchEvent(new Event("change", { bubbles: true }));
      formElem.requestSubmit();
    }
  });

  formElem.addEventListener("submit", (e) => {
    // Scroll the last message into view
    var lastMessage = document.querySelector(".messages-scroller").lastChild;
    lastMessage.scrollIntoView({ behavior: "instant", block: "end" });
  });
}

export function autoResizeTextarea(textareaId, minRows = 3, maxRows = 5) {
  const textarea = document.getElementById(textareaId);
  if (!textarea) return;

  const adjustTextareaRows = () => {
    const lineHeight = parseFloat(getComputedStyle(textarea).lineHeight);
    textarea.rows = minRows;
    const currentRows = Math.floor(textarea.scrollHeight / lineHeight);
    textarea.rows = Math.min(currentRows, maxRows);
  };

  textarea.addEventListener("input", adjustTextareaRows);
}
