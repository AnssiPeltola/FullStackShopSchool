// Add an event listener to each checkbox
document.querySelectorAll('input[type="checkbox"]').forEach((checkbox) => {
  checkbox.addEventListener("change", function () {
    // Tests if the checkbox is checked
    // console.log('Checkbox changed');

    // If the checkbox is checked, display the next input field. Otherwise, hide it.
    this.nextElementSibling.nextElementSibling.style.display = this.checked
      ? "block"
      : "none";
  });
});

// Add an event listener to the form submission
document
  .querySelector("#updateForm")
  .addEventListener("submit", function (event) {
    // Prevent the form from submitting normally
    event.preventDefault();

    // Get the product name from the form
    let productName = document.querySelector("#productName").value;
    // Get all checked checkboxes from the form
    let fields = document.querySelectorAll(
      "#updateForm input[type='checkbox']:checked"
    );
    // Initialize an object to hold the updates
    let updates = {};

    // For each checked checkbox, add the corresponding input field's value to the updates object
    fields.forEach((field) => {
      let valueInput = document.querySelector(
        `#updateForm input[name='${field.value}']`
      );
      updates[field.value] =
        field.value === "hinta"
          ? parseFloat(valueInput.value)
          : valueInput.value;
    });

    // Send a PUT request to the server with the updates
    fetch(`${baseUrl}/updateproduct/${productName}`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(updates),
    })
      .then((response) => {
        // If the response is not OK, throw an error
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        } else if (
          response.headers.get("content-type") &&
          response.headers.get("content-type").includes("application/json")
        ) {
          // If the response is OK and the content type is JSON, parse the response data as JSON
          return response.json();
        } else {
          // If the response is OK but the content type is not JSON, return undefined
          return;
        }
      })
      .then((data) => {
        // If there is data, log it to the console
        if (data) {
          console.log(data);
        }

        // Clear all checkboxes and input fields after the form submission
        document
          .querySelectorAll('input[type="checkbox"]')
          .forEach((checkbox) => {
            checkbox.checked = false;
            checkbox.nextElementSibling.nextElementSibling.style.display =
              "none";
          });
        document
          .querySelectorAll(
            'input[type="text"], input[type="number"], input[type="file"]'
          )
          .forEach((input) => {
            input.value = "";
          });
      })
      .catch((error) => console.error("Error:", error)); // Log any errors to the console
  });
