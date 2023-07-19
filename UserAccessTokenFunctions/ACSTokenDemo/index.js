const { CommunicationIdentityClient } = require('@azure/communication-identity');


const connectionString = process.env.ACS_CONNECTION_STRING;

module.exports = async function (context, req) {
    let tokenClient = new CommunicationIdentityClient(connectionString);

    const user = await tokenClient.createUser();

    const userToken = await tokenClient.getToken(user, ["voip"]);

    context.res = {
        body: userToken
    };
}
