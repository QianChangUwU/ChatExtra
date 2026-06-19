use serde::{Deserialize, Serialize};
use url::Url;

#[derive(Debug, Deserialize, Serialize)]
pub struct Config {
    pub server: Server,
    pub database: Database,
    #[serde(default)]
    pub influx: Option<Influx>,
    #[serde(default)]
    pub registration: Option<Registration>,
}

#[derive(Debug, Deserialize, Serialize)]
pub struct Registration {
    #[serde(default = "default_verify")]
    pub verify_on_lodestone: bool,
}

fn default_verify() -> bool {
    true
}

#[derive(Debug, Deserialize, Serialize)]
pub struct Server {
    pub address: String,
}

#[derive(Debug, Deserialize, Serialize)]
pub struct Database {
    pub path: String,
}

#[derive(Debug, Deserialize, Serialize)]
pub struct Influx {
    pub url: Url,
    pub org: String,
    pub bucket: String,
    pub token: String,
}
