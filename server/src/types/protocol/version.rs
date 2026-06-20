use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct VersionRequest {
    pub version: u32,
    #[serde(default)]
    pub client_version: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct VersionResponse {
    pub version: u32,
    #[serde(default)]
    pub required_version: String,
}
