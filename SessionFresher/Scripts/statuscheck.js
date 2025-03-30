const axios = require('axios');
const https = require('https');

const agent = new https.Agent({  
  rejectUnauthorized: false
});

(async () => {
  try {
    const response = await axios.get('https://your-api/statuscheck', { httpsAgent: agent });
    if (response.status === 200) {
      console.log('Status check passed.');
      process.exit(0);
    } else {
      console.error(`Unexpected status: ${response.status}`);
      process.exit(1);
    }
  } catch (error) {
    console.error('Error calling /statuscheck:', error.message);
    process.exit(1);
  }
})();
