use std::sync::Arc;

use anyhow::Context;
use tokio::sync::RwLock;

use crate::{ClientState, State, util, WsStream};
use crate::types::protocol::{NicknameRequest, NicknameResponse};

pub async fn nickname(state: Arc<RwLock<State>>, client_state: Arc<RwLock<ClientState>>, conn: &mut WsStream, number: u32, req: NicknameRequest) -> anyhow::Result<()> {
    let lodestone_id = match client_state.read().await.lodestone_id() {
        Some(id) => id as i64,
        None => return Ok(()),
    };

    sqlx::query!(
        // language=sqlite
        "update users set nickname = ? where lodestone_id = ?",
        req.nickname,
        lodestone_id,
    )
        .execute(&state.read().await.db)
        .await
        .context("could not update nickname")?;

    client_state.write().await.user.as_mut().unwrap().nickname = req.nickname.clone();

    util::send(conn, number, NicknameResponse {
        nickname: req.nickname,
    }).await
}
