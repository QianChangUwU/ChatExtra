use anyhow::Result;

use crate::{
    types::protocol::{
        VersionRequest,
        VersionResponse,
    },
    util::send,
    WsStream,
};

const VERSION: u32 = 1;
const SERVER_CLIENT_VERSION: &str = env!("EXTRACHAT_VERSION");

#[allow(unused_variables)]
pub async fn version(conn: &mut WsStream, number: u32, req: VersionRequest) -> Result<bool> {
    send(conn, number, VersionResponse {
        version: VERSION,
        required_version: SERVER_CLIENT_VERSION.to_string(),
    }).await?;

    Ok(true)
}
