use lodestone_scraper::lodestone_parser::ffxiv_types::World;

#[derive(Debug, Clone)]
pub struct User {
    pub lodestone_id: u64,
    pub name: String,
    pub world: World,
    pub raw_world: Option<u16>,
    pub hash: String,
    pub nickname: Option<String>,
}

impl User {
    pub fn world_id(&self) -> u16 {
        self.raw_world.unwrap_or_else(|| crate::util::id_from_world(self.world))
    }
}
