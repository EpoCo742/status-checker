const axios = require('axios');

const API_URL = 'http://localhost:5193/api/statuscheck';

(async () => {
  try {
    const response = await axios.get(API_URL);
    if (response.status === 200) {
      console.log('Status check passed. Exiting.');
      process.exit(0);
    } else {
      console.error(`Received unexpected status: ${response.status}`);
      process.exit(1); 
    }
  } catch (error) {
    console.error('Error calling /statuscheck:', error.message);
    process.exit(1);
  }
})();