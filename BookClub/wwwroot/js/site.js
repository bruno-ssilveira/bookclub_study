document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll("[data-copy-text]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const text = btn.getAttribute("data-copy-text");
      if (!text) return;

      const originalHtml = btn.innerHTML;
      try {
        await navigator.clipboard.writeText(text);
        btn.innerHTML = '<i class="bi bi-check2-circle" aria-hidden="true"></i> Copiado!';
        btn.classList.remove("btn-bc-outline");
        btn.classList.add("btn-success");
        setTimeout(() => {
          btn.innerHTML = originalHtml;
          btn.classList.remove("btn-success");
          btn.classList.add("btn-bc-outline");
        }, 2200);
      } catch {
        const temp = document.createElement("input");
        temp.value = text;
        document.body.appendChild(temp);
        temp.select();
        document.execCommand("copy");
        document.body.removeChild(temp);
        btn.innerHTML = '<i class="bi bi-check2-circle" aria-hidden="true"></i> Copiado!';
        setTimeout(() => {
          btn.innerHTML = originalHtml;
        }, 2200);
      }
    });
  });

  document.querySelectorAll("input[data-uppercase]").forEach((input) => {
    input.addEventListener("input", () => {
      const pos = input.selectionStart;
      input.value = input.value.toUpperCase();
      if (pos !== null) {
        input.setSelectionRange(pos, pos);
      }
    });
  });
});
