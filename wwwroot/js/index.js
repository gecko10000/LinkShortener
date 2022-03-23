var form = $("#linkForm");
form.submit(function (e) {
  e.preventDefault();
  $.ajax({
    type: "POST",
    url: "/set",
    data: form.serialize(),
    success: function(data) {
      show(document.getElementById("link-button"));
      show(document.getElementById("stats-button"));
      var output = document.getElementById("shortened");
      show(output);
      link = "https://" + window.location.host + "/" + data;
      output.innerHTML = link;
      document.getElementById("stats-link").href = link + "/stats";
      document.getElementById("forCopy").value = link;
      output.setAttribute("href", link);
    }
  })
});

function show(el) {
  el.style.visibility = "visible";
}

function copyToClipboard() {
  var text = document.getElementById("forCopy");
  text.select();
  text.setSelectionRange(0, 99);

  navigator.clipboard.writeText(text.value);
}