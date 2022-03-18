var form = $("#linkForm");
form.submit(function (e) {
  e.preventDefault();
  $.ajax({
    type: "POST",
    url: "/set",
    data: form.serialize(),
    success: function(data) {
      var output = document.getElementById("shortened");
      show(output);
      link = "https://" + window.location.host + "/" + data;
      output.innerHTML = link;
      output.setAttribute("href", link);
    }
  })
});

function show(el) {
  el.style.visibility = "visible";
}